# Progress / Handoff

A running journal: what shipped per version, decisions that *aren't* captured in a build plan,
and what's next. Read the **build plans** for scope, rationale, wireframes, and per-milestone test
lists; read **this** for current state and the cross-cutting knowledge the plans can't carry.

**Plan versions**
- [build-plan-1.0.md](build-plan-1.0.md) — frozen v1 brief (M0–M5). Shipped.
- [build-plan-1.1.md](build-plan-1.1.md) — singles improvements (M6–M10). Shipped.
- [build-plan-1.2.md](build-plan-1.2.md) — archived releases (M11). Shipped.
- [build-plan-2.0.md](build-plan-2.0.md) — songs & catalog (M12–M15). **M12–M15 shipped — v2.0 complete.**
- [build-plan-2.1.md](build-plan-2.1.md) — UX refinements (M16–M18). **M16–M18 shipped — v2.1 complete.**
- [build-plan-2.2.md](build-plan-2.2.md) — UX improvements (M19–M23). **M19–M21 + M23 shipped; M22 next.**

Newer plan versions go in new `build-plan-N.N.md` files; older ones stay frozen.

**Current state:** **build-plan-2.0 fully shipped** (M12–M15) — songs, catalog, pending rework, and the
archive cascade — plus a round of post-v2.0 UX/feature improvements, and now **build-plan-2.1 fully
shipped** (M16–M18): the `Modal`/confirm-dialog primitives, toast variants, and the `SongPickerModal` +
unified `Tracklist`, plus a bugfix round enforcing **per-artist song-title uniqueness** and an **immutable song
main artist**. Then **build-plan-2.2 M19** shipped — the Artists redesign (table · smart delete · dedicated
pages), plus a fix to the artist delete guard (feat/collab credits now block deletion too). Tests green
(`dotnet test` — domain 62 / API 102). Next work is **build-plan-2.2 M20–M23** (see
[Backlog / next steps](#backlog--next-steps)).

> ⚠️ **M12 is a hard schema reset with no migration.** Any existing local `src/Zmg.Api/zmg.db` from
> v1.x must be deleted before running — the fresh `InitialCreate` won't apply on top of the old schema.
> Delete it (`rm src/Zmg.Api/zmg.db*`) and startup will recreate a seeded db.

---

## Journal

**v1 (M0–M5) — foundation.** Domain (entities, template-copy, progress, derived status, validation,
seed), the minimal API + EF/SQLite with seeded templates, and the React SPA (dashboard, artists,
release form + detail checklist, templates editor, album tracklist). M5 was polish: 375px mobile
pass, filters, empty states, and the multi-stage Dockerfile.

**v1.1 (M6–M10) — singles improvements.** UPC/ISRC + the soft "missing identifier" warning (only
after *Distribute to DSPs* is checked); per-task timeframes (`MinDaysBefore`/`MaxDaysBefore`, max
drives calc, Pre-only) + surfaced notes; the dashboard split into **Home** (`scope=home`,
forward-looking) and **All Releases** (`scope=all` table); and the **pending-actions** engine
(`PendingActions.Compute` → `GET /api/pending` + a detail "Needs attention" block). The single
template grew 30 → **31** (Distribute inserted as 3rd Pre); album unchanged at **40**.

**v1.2 (M11) — archived lifecycle.** `Archived` release status + a soft-delete ("Remove"):
`ArchivedAt`/`DeletedAt` on Release, `POST /api/releases/{id}/archive` (guarded to `releaseDate >=
today`, not-twice), `DELETE /api/releases/{id}` repurposed to a guarded soft-delete (archived only),
`scope=archived`, the `/releases/archived` page (linked from All Releases, not a nav item), Archive
on Home cards + All Releases rows, and a read-only archived detail. Verified end-to-end via the
running API + browser SPA.

**v2.0 (M12–M15) — songs, catalog, pending rework & archive cascade.** A first-class **Song**
([Song.cs](src/Zmg.Domain/Entities/Song.cs)) — title, main artist, ISRC, feats/collabs
([SongArtist.cs](src/Zmg.Domain/Entities/SongArtist.cs), replacing `ReleaseArtist`), own archive
lifecycle — split from **Release** (UPC, cover, tasks); they link through a pure **`Track` join**
(composite PK `(ReleaseId, SongId)`, endpoints `/api/releases/{releaseId}/tracks/{songId}`; the old
`/api/tracks/{id}` group is gone). **Hard schema reset:** all v1.x migrations dropped for one
`InitialCreate`, no upgrade path; Release lost `Isrc`/`FeaturedArtists` (UPC-only warning);
auto-distribute is past-date-only. **Catalog** (`SongService`/`SongEndpoints`): `/catalog` list +
`/catalog/:id` detail with a **derived** release date/UPC list (earliest non-archived link, null for
orphans), always-editable song fields (rename-clash → non-blocking warning), an **artist-drift hint**,
and a debounced `SongPicker` wired into the create-form Tracks section + album add-row (replaced by
`SongPickerModal` in M18); a single collapses to a compact `SongCard`. **Pending
rework:** `MissingIdentifier` split into release-owned **`MissingUpc`** + song-owned **`MissingIsrc`**,
plus an **`EmptyAlbum`** kind; `PendingAction` carries nullable `ReleaseId`/`SongId` + a generic
`Subject`; one action per song ([`PendingActions.ComputeForSong`](src/Zmg.Domain/PendingActions.cs));
`PendingSection` is collapsible with per-kind routing. **Archive cascade:** archiving a release cascades
to its dormant songs (pure [`SongArchival.ShouldArchive`](src/Zmg.Domain/SongArchival.cs) — not archived,
never released, no remaining active link; released/shared songs stay put); manual
`POST /api/songs/{id}/archive` + soft-delete `DELETE /api/songs/{id}` (archived or orphan only),
`PUT /api/songs/{id}` 409s when archived, and read-only archived Catalog pages
([`ArchivedSongsPage`](src/Zmg.Web/src/features/catalog/ArchivedSongsPage.tsx)). Shipped green — see
[build-plan-2.0.md](build-plan-2.0.md).

**Post-v2.0 improvements.** A round of small UX/feature changes: client-side release-form validation
(red fields for missing title/date via a `Field` `error` prop); a **Statuses filter** on All Releases;
**add songs directly in the catalog** (`POST /api/songs` →
[`SongService.CreateAsync`](src/Zmg.Api/Services/SongService.cs), an orphan song guarded by the
one-artist rule; **+ New song** at `/catalog/new`); an **artist filter** on the Catalog; and an
**archive-cascade warning** (`GET /api/releases/{id}/archive-preview` →
[`GetArchivePreviewAsync`](src/Zmg.Api/Services/ReleaseService.cs) lists the songs that will cascade,
surfaced in every archive confirm via [`archiveReleaseConfirmMessage`](src/Zmg.Web/src/features/releases/archiveConfirm.ts)).
Then **release warnings were consolidated**: the per-warning DTO booleans collapsed into one
`warnings: string[]` (pure [`ReleaseWarnings.Compute`](src/Zmg.Domain/ReleaseWarnings.cs) → "Missing UPC" /
"Album is empty" / "Album has only 1 track"), rendered by a single amber
[`SoftWarning`](src/Zmg.Web/src/components/SoftWarning.tsx) icon (renamed from `IdentifierWarning`) that
lists them all on click. Tests green (domain 62 / API 95); a live `dotnet run` smoke test stays blocked
by the documented EF-tooling migration issue below, not by any of these changes.

**v2.1 (M16–M18) — UX refinements + follow-on integrity fixes.** The app's overlay layer plus a catalog-integrity
pass. **Overlays (M16):** [`Modal`](src/Zmg.Web/src/components/Modal.tsx) portals to `<body>` (bottom sheet on
mobile, centered card from `sm`; Escape/backdrop close, body scroll-lock, panel focus **guarded** so a child's
`autoFocus` keeps it — a focus-steal that had defeated the picker's search input); on it,
[`ConfirmDialog`](src/Zmg.Web/src/components/ConfirmDialog.tsx) + [`ConfirmProvider`](src/Zmg.Web/src/hooks/ConfirmProvider.tsx)/[`useConfirm`](src/Zmg.Web/src/hooks/useConfirm.ts)
give a promise-based `confirm(opts) => Promise<boolean>` from one instance at the `App.tsx` root — **all 13
`confirm()` + 7 `alert()` calls are gone** (alerts → red error toasts; `archiveConfirm.tsx` returns full
`ConfirmOptions` with a `ReactNode` cascade-song `<ul>`). **Toasts (M17):** [`Toast`](src/Zmg.Web/src/components/Toast.tsx)
gained `variant: success|error|info` (`error` stays the default so failure callers don't change), fixing
`SongDetailPage`'s post-save toast to a green "✓ Saved."; slide-in via a real `toast-in` keyframe in
`tailwind.config.js` (both frames must carry `translate(-50%)` or the centered toast jumps). New amber **`archive`
Button/`MenuItem` variant** (terminal ≠ destructive) on every archive action, red kept for deletes. **Tracklist
(M18):** the inline `SongPicker` became [`SongPickerModal`](src/Zmg.Web/src/features/catalog/components/SongPickerModal.tsx)
(browses on open, always artist-scoped) and the album list unified into one
[`Tracklist`](src/Zmg.Web/src/features/releases/components/Tracklist.tsx) for create form + detail (deleting
`TrackRow` + its kebab; `onToggleFocus` optional). **Follow-ons:** a new-song row collects ISRC/feats at add time via
[`NewTrackForm`](src/Zmg.Web/src/features/releases/components/NewTrackForm.tsx) (no backend change — `TrackInput`
already carried them); **song titles became unique per main artist** — [`Validation.ValidateSong`](src/Zmg.Domain/Validation.cs)
now errors (was a soft warning), enforced across `SongService` create/rename, `ReleaseService.CreateAsync` inline
tracks, and `TrackService.AddAsync`, with a "Song already exists" modal on the detail page offering *Add existing* /
*Change the name*; and a song's **main artist is immutable** ([`SongService.UpdateAsync`](src/Zmg.Api/Services/SongService.cs)
409s, detail renders it read-only). **Tooling:** oxlint → **ESLint 9** (flat `eslint.config.js`; oxlint's native
binding never installed), which meant splitting non-component exports into their own modules (`emptyTrack` →
`trackInput.ts`; the confirm context/hook `useConfirm.ts` apart from `ConfirmProvider.tsx`) for clean Fast-Refresh
boundaries. Browser-verified end-to-end (sheet vs. card + Escape/backdrop, cross-artist picker scope at API **and**
UI with a two-artist seed, both duplicate-modal branches, read-only main artist); `dotnet test` green (domain 62 /
API 99), `npm run lint`/`build` clean.

**v2.2 M19 — Artists redesign (table · smart delete · dedicated pages).** The hand-rolled `divide-y` artist
list became a bordered table (**Name · Releases · Songs · Actions**) matching Catalog/Releases, each row a
`RowMenu` kebab. **Backend slice:** `ArtistDto` gained `SongCount` (and `CreditCount`, see below);
`ArtistService.ListAsync`/new `GetAsync(id)` project both counts, `GET /api/artists/{id}` added
([ArtistEndpoints.cs](src/Zmg.Api/Endpoints/ArtistEndpoints.cs)); `api/artists.ts` got `get()`. **Smart delete
(no post-hoc toast):** the row carries the counts, so the page branches *before* asking — an **info** modal
(`confirm({ confirmLabel:'OK', hideCancel:true })`, result ignored) when the artist is still referenced, a red
**Delete** confirm when clean. New optional `hideCancel?: boolean` on
[`ConfirmOptions`](src/Zmg.Web/src/components/ConfirmDialog.tsx) renders only the confirm button. **Dedicated
pages:** new [`ArtistFormPage`](src/Zmg.Web/src/features/artists/ArtistFormPage.tsx) (mirrors `SongFormPage`;
`/artists/new` create + `/artists/:id` edit via `api.artists.get`), retiring the inline `ArtistForm.tsx`
(deleted). **Bug found & fixed while verifying:** the delete guard counted only main-artist references
(releases + songs), **not feat/collab credits** (`SongArtist`, a Restrict FK) — a feat-only artist slipped past
the guard and **500'd on the FK** instead of a clean conflict, defeating M19's "check up front" promise.
`ArtistService.DeleteAsync` now also counts `SongArtists`; `ArtistDto.CreditCount` surfaces it so the info modal
blocks feat-only artists up front too ("still tied to N feat/collab credits"). Browser-verified end-to-end
(table + counts, both delete branches incl. the credit case, create → list, edit prefill); `dotnet test` green
(domain 62 / **API 102** — +GET-by-id/songCount/feat-delete-guard), `npm run lint`/`build` clean.

**v2.2 M21 + M23 — compact card · one reorder control.** Two SPA-only milestones (M22, the calendar, was
skipped for now and is the only 2.2 work left). **M21:** the Home card moved to
[`features/releases/components/ReleaseCard.tsx`](src/Zmg.Web/src/features/releases/components/ReleaseCard.tsx)
— it's no longer Home's, since the M22 calendar preview will render it too. Its Edit/Archive `Button` pair
collapsed into the same `RowMenu` kebab the M20 tables use (Archive gated on `releaseDate >= todayIso()`,
matching `AllReleasesPage`, where before Home showed it unconditionally) and padding tightened
`p-4`→`p-3`/`gap-3`→`gap-2`. **Two independent display flags, both default-off:** `showCover` (Home passes it —
the cover *is* its click-to-open affordance) and `showOpenLink` (the coverless calendar preview will pass it for
an explicit "Open release →"; Home doesn't need it). Matching the mockup also meant a **slim `ProgressBar` +
"X / Y tasks"** — added optional `slim`/`label` props rather than a second bar, so `ReleaseHeader` keeps the
default "N/M done · P%" — and **`formatReleaseDate`** ("Aug 22, 2026") in `lib/format.ts`, parsing local
midnight (`+'T00:00:00'`) like `daysToRelease`, since bare `new Date('yyyy-MM-dd')` is UTC and drifts a day
back in negative offsets — the same trap the M22 calendar plan flags for its cells. **M23:** the
checklist rows' kebab "Move up"/"Move down" items are gone, replaced by inline ↑/↓ — extracted as shared
[`ReorderArrows`](src/Zmg.Web/src/components/ReorderArrows.tsx) (`{ isFirst, isLast, onMove }`, disabled at
the ends) and adopted by `TaskRow`, `TemplateTaskRow`, and `Tracklist` (which had owned the only copy of that
markup). Arrows stay behind `!readOnly`, so the archived read-only detail shows none. `npm run lint` +
`npm run build` clean; no backend touched, so `dotnet test` was deliberately not re-run (SPA-only blast radius).
**Not yet browser-driven** — the kebab-on-card and the arrows want a visual pass at 375px and desktop.

---

## Cross-cutting decisions (not in any single plan)

- **Status is derived, never stored** — recomputed from tasks + date on every read. The **one**
  exception is `Archived` (v1.2), a persisted flag that overrides the derived value.
- **Soft-delete, never hard-delete** (v1.2). Removed releases are stamped `DeletedAt` and hidden by a
  global query filter (`HasQueryFilter(r => r.DeletedAt == null)`) so stable ids survive for phase-2
  stats. EF logs a benign "required end of a relationship" advisory for the child navs.
- **Template-copy-on-create is backend logic, wired since v1** — a release is born with a full
  snapshot checklist. Editing a template never touches existing releases (covered by
  `TemplateApiTests.Editing_a_template_does_not_touch_existing_releases`).
- **Reorder is move-up/move-down, not drag-and-drop.** The order endpoint takes the full ordered id
  list for a phase; the UI posts a single-swap result. Dependency-free, mobile-friendly; the endpoint
  already supports arbitrary orderings if DnD is added later. **Its one UI control is inline ↑/↓ via
  `components/ReorderArrows.tsx` (M23)** — never a kebab item, never a second copy of the arrow markup.
- **Mutations return the single changed DTO** (or 204); the detail screen holds a flat task array and
  recomputes phase groups + progress client-side, so no re-fetch. Moving a task across phases appends
  to the target (`SortOrder = max+1`).
- **Tracks key off `TrackNumber`** (1-based, contiguous) for both order and display; reorder rewrites
  it, delete renumbers survivors. Tracklist is UI-gated to `type === Album` (endpoints aren't hard-scoped).
- **Create/update release responses are `{ data, warnings }`** (`CreatedWithWarnings<T>`) so
  non-blocking validation warnings reach the form.
- **Release advisories are one array, computed in one place (post-v2.0).** Soft warnings (Missing UPC,
  empty/thin album) ship as `warnings: string[]` on the release list/detail DTOs, built by pure
  `ReleaseWarnings.Compute` and rendered by a single `SoftWarning` icon that lists them all. Add a new
  advisory *there*, not as another DTO boolean. Distinct from `CreatedWithWarnings` (create/update
  *validation* warnings).
- **No native dialogs (M16).** `window.confirm`/`window.alert` are banned app-wide: ask with
  `useConfirm()`'s `confirm(opts)` (one `<ConfirmDialog>` under the root `ConfirmProvider`), report failures
  with an error toast. Overlays build on `components/Modal.tsx` rather than hand-rolling a backdrop.
  Destructive intent is colour-coded: red `danger` for hard deletes, amber `archive` for archiving.
- **One tracklist, two adapters (M18).** `features/releases/components/Tracklist.tsx` owns the album row
  design and controls for *both* the create form and the release detail; neither context gets its own row
  markup. It holds no persistence — `TracksEditor` (local `EditorRow[]`) and `ReleaseDetailPage` (optimistic
  `api.tracks.*`) adapt to it. Singles are deliberately outside it: one fixed row, nothing to reorder.
  Linking an existing song always goes through `SongPickerModal`, which is **always scoped to the release's
  main artist** — never widen it to the whole catalog.
- **macOS is case-insensitive — `Foo.tsx` and `foo.tsx` are one file.** M18 renamed `TrackList.tsx` →
  `Tracklist.tsx`; writing the "new" file just overwrote the old one. Use `git mv` for case-only renames.
- **Buttons inside a `<form>` need an explicit `type`** — the HTML default is `submit`. Shared components
  that may be rendered inside a form (`InlineAddForm`, and anything reused from `components/`) set
  `type="button"`; `Button` itself has no default, so real submits stay explicit (`type="submit"`).
- **Enums serialize as integers** (System.Text.Json default); the TS layer mirrors (`ReleaseType.Single
  = 0`, …). Change both sides together. `erasableSyntaxOnly` is disabled in `tsconfig.app.json` so TS
  `enum`s compile.
- **Web is organized by feature folder** (`src/Zmg.Web/src/features/{home,releases,artists,templates}`),
  not flat `pages/` — an earlier refactor (#1). Shared UI in `components/`, API client in `api/`,
  types in `types/`.
- **Song vs Release (v2.0).** A **Song** is the creative work (title, ISRC, feats/collabs, main
  artist); a **Release** is the commercial package (UPC, cover, tasks). They link through `Track`, so
  one song can sit on a single *and* an album. A song's **UPCs and release date are derived** from its
  links, never stored. **Type is fixed at create** (determines the checklist) and PUT rejects a change
  with 409. A single is fixed at one track; an album has zero+. Song fields of an **existing** song are
  edited only on the catalog detail page (M13) — release-detail track rows just reorder/focus/remove. The
  one exception is **creation**: adding a *new* song from a tracklist may set its title/ISRC/feats at add
  time (create form's per-row disclosure, and the detail page's `NewTrackForm`), since that's the song's
  birth, not later editing.
- **Song titles are unique per main artist; a song's main artist is immutable (v2.1).** A title must be
  unique among an artist's active (non-archived) songs — a **hard error** in pure `Validation.ValidateSong`
  (shared `DuplicateSongTitleMessage`), enforced at *every* mint path (song create/rename,
  `ReleaseService.CreateAsync` inline tracks, `TrackService.AddAsync`), never a soft warning. The
  release-detail "+ Add track" surfaces a clash as a modal offering the existing song or a rename. A song's
  **main artist can't change after creation** (`SongService.UpdateAsync` 409s) — it may already sit on that
  artist's releases; the catalog detail renders it read-only.
- **Hard schema reset, no migration (v2.0).** M12 deleted all v1.x migrations and regenerated a single
  `InitialCreate`; there is intentionally **no upgrade path** from a v1.x db. Delete any local `zmg.db`.
  Template/task `HasData` seeding carried over unchanged (proven by the green seed tests).
- **EF tooling must match the runtime (EF 8).** v2.0 needs no further migrations, so nothing is pinned
  in-repo. **If** you ever regenerate one, use a `dotnet-ef` on the 8.x line — a 10.x-generated
  migration builds fine but silently fails at runtime (`no such table: __EFMigrationsHistory` on the
  history insert). Install matching tooling first, e.g. `dotnet tool install --global dotnet-ef --version 8.0.11`.
- **Track query filter (v2.0).** A join between two soft-filtered entities needs its own filter
  (`t.Release!.DeletedAt == null && t.Song!.DeletedAt == null`) so a stale join vanishes with either
  parent; it also silences EF's required-nav-to-filtered-entity advisory for `Track`. The same benign
  advisory still logs for `Release→ReleaseTask` and now `Song→SongArtist` (expected).

## Run

```bash
# dev (two terminals)
dotnet run --project src/Zmg.Api                 # API on :5274 (default profile)
cd src/Zmg.Web && npm install && npm run dev     # SPA on :5173, proxies /api → :5274

# prod-style (one process)
cd src/Zmg.Web && npm run build                  # → src/Zmg.Api/wwwroot
dotnet run --project src/Zmg.Api                 # serves SPA + API together

dotnet test                                      # full suite
```

DB is `src/Zmg.Api/zmg.db` (git-ignored, migrated on startup). Delete it to reset.

## Project layout

```
src/Zmg.Domain   entities/enums, template-copy, progress, status, validation, seed,
                 release-warnings, song-archival, pending-actions  (pure, no I/O)
src/Zmg.Api      minimal API: endpoints, service layer (+ interfaces), DTO contracts, extensions
src/Zmg.Infra    EF Core + SQLite: ZmgDbContext (seeding) + migrations
src/Zmg.Web      React + Vite + Tailwind SPA, organized by feature folder
tests/Zmg.Domain.Tests   xUnit unit tests
tests/Zmg.Api.Tests      integration tests (WebApplicationFactory + in-memory SQLite)
```

---

## Backlog / next steps

- **Shipped:** v2.0 (M12–M15) and **build-plan-2.1 (M16–M18)** — overlays/confirm, toast variants,
  `SongPickerModal` + unified `Tracklist`, plus the song-uniqueness/immutable-artist integrity fixes and the
  ESLint migration. All browser-verified; see the journal. One thing never driven in the browser: the
  archive-confirm cascade *list* specifically (needs a release with dormant cascading songs) — the underlying
  `ConfirmDialog`/`Modal` + `ReactNode` body are otherwise verified.
- **build-plan-2.2 — UX improvements (M19–M23). M19–M21 + M23 shipped; only M22 (calendar) left.** See
  [build-plan-2.2.md](build-plan-2.2.md) for full scope, mockup notes, and per-milestone test lists.
  - **M19 — Artists redesign. ✅ Shipped.** Table (Name · Releases · Songs · Actions) + `RowMenu` kebab;
    up-front smart delete (info modal vs. red confirm, `ConfirmOptions.hideCancel`); dedicated
    `/artists/new` + `/artists/:id` pages (`GET /api/artists/{id}`), inline `ArtistForm` deleted. Also fixed:
    the delete guard now counts feat/collab credits (`ArtistDto.CreditCount`) so a feat-only artist blocks
    with a clean 409 instead of a FK 500.
  - **M20 — Kebab menus. ✅ Shipped.** on the Releases + Catalog table rows (replace inline Archive/Delete/Edit buttons).
  - **M21 — Compact `ReleaseCard`. ✅ Shipped.** Moved to `features/releases/components/ReleaseCard.tsx`
    (old `features/home/components/ReleaseCard.tsx` deleted); kebab actions, `showCover` opt-in, ready for
    the M22 calendar preview.
  - **M22 — Releases calendar view.** A Table/Calendar toggle + hand-rolled month grid (`lib/calendar.ts`),
    dependency-free; opens on today's month, "Next release" jump chip (hidden when nothing upcoming), mobile
    dots vs desktop chips; click → preview modal of compact cards. **No backend change** (`scope=all` already
    returns all dates).
  - **M23 — Inline reorder arrows. ✅ Shipped.** New shared
    [`ReorderArrows`](src/Zmg.Web/src/components/ReorderArrows.tsx) on `TaskRow` + `TemplateTaskRow`
    (arrows gated behind `!readOnly`), the kebab's "Move up/down" items dropped, and `Tracklist`
    refactored onto it — one reorder control app-wide.
  - Branch `feat/v2.2-ux-improvements` exists; only the build-plan doc + this note are committed-in-spirit —
    no implementation code written yet.
- **Phase 2 — DSP stats (after v2.2)** (the reason this exists over Notion/Trello): hang streaming/revenue data
  off the stable Artist / Release / **Song** / Track ids and UPC/ISRC columns. The v2.0 Song ids are its
  foundation. No build plan yet — write `build-plan-3.0.md` when it starts.
- **Per-track task fan-out** on albums: registrations that repeat per track are single "per track"
  tasks today. Decide after the first real album.
- **Verify the Docker image** on a machine with the daemon running (`docker build -t zmg-tracker .`) —
  written and reviewed, never built here (daemon was down).
- Deferred: un-archive/restore and hard-delete/purge (archives are terminal by rule); auth for hosted
  deploys; absolute per-task due dates (v1.1 only added timeframe *ranges*).

**Env note:** the linter is **ESLint** (flat config `src/Zmg.Web/eslint.config.js`) — `npm run lint`
runs clean. It replaced oxlint, whose native binding never installed locally (`@oxlint/binding-darwin-x64`
missing — the npm optional-deps bug, same class as the Vite 8 rolldown binding, which is why Vite stays
pinned to 7.x).
