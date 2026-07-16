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

Newer plan versions go in new `build-plan-N.N.md` files; older ones stay frozen.

**Current state:** **build-plan-2.0 fully shipped** (M12–M15) — songs, catalog, pending rework, and the
archive cascade — plus a round of post-v2.0 UX/feature improvements, and now **build-plan-2.1 fully
shipped** (M16–M18): the `Modal`/confirm-dialog primitives, toast variants, and the `SongPickerModal` +
unified `Tracklist`, plus a bugfix round enforcing **per-artist song-title uniqueness** and an **immutable song
main artist**. Tests green (`dotnet test` — domain 62 / API 99). Next work is **Phase 2 (DSP stats)**
(see [Backlog / next steps](#backlog--next-steps)).

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
and a debounced [`SongPicker`](src/Zmg.Web/src/features/catalog/components/SongPicker.tsx) wired into the
create-form Tracks section + album add-row; a single collapses to a compact `SongCard`. **Pending
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

**v2.1 (M16) — Modal primitive + custom confirm dialog.** The app's first real overlay:
[`Modal`](src/Zmg.Web/src/components/Modal.tsx) portals to `<body>` (bottom sheet on mobile, centered card
from `sm` up; Escape/backdrop close, body scroll-lock, panel focused on open). On top of it,
[`ConfirmDialog`](src/Zmg.Web/src/components/ConfirmDialog.tsx) +
[`ConfirmProvider`/`useConfirm`](src/Zmg.Web/src/hooks/useConfirm.tsx) give a promise-based
`confirm(opts) => Promise<boolean>` from one dialog instance mounted at the `App.tsx` root, so call sites
still read `if (!(await confirm({...}))) return;`. **All 13 `confirm()` and 7 `alert()` calls are gone** —
alerts became red error toasts, which meant `useToast` + `<Toast>` also landed on Home, All/Archived
Releases, Catalog, Archived Songs, and Artists. New amber **`archive` Button variant** (terminal but not
destructive) now on every archive action — Home card, All Releases, release detail, Catalog — with red kept
for deletes; `MenuItem`'s boolean `danger` became `tone: 'default' | 'danger' | 'archive'` to match (3 call
sites updated). `archiveConfirm.ts` → **`.tsx`**: `archiveReleaseConfirmMessage` (a `\n`-joined string) is now
`archiveReleaseConfirm`, returning a whole `ConfirmOptions` with a `ReactNode` body that renders the cascade
songs as a real `<ul>` — it returns the full options rather than just the body because the title/label/variant
were identical at all three call sites. Verified: `tsc -b` + `npm run build` clean, `dotnet test` green
(62/95), and — contrary to the note below — `dotnet run` now boots fine and serves the SPA + API on :5274
(`/` and `/api/releases` both 200), with the amber utilities confirmed in the CSS bundle. The interactive
click-through (sheet vs. card, Escape/backdrop) is **not** yet done — no browser automation here.

**v2.1 (M17) — toast variants.** [`Toast`](src/Zmg.Web/src/components/Toast.tsx) gains
`variant: 'success' | 'error' | 'info'` (emerald + ✓ / red / slate), and `useToast`'s
`showToast(msg, variant = 'error')` stores `{ message, variant }` — **`error` stays the default**, so the many
revert/failure callers (and M16's `alert`→toast replacements) keep their red with no edit. The hook still
returns `toast` as a plain string and adds `toastVariant`, so the only change at the 9 render sites is passing
`variant={toastVariant}`. The actual fix: `SongDetailPage`'s post-save `showToast('Saved.', 'success')` is now
green — it was a red pop-up that read as failure. The slide-in is a real `toast-in` keyframe in
`tailwind.config.js` (not an arbitrary value) because the toast centers via `-translate-x-1/2` and an
**animation transform replaces the class's rather than composing with it** — both frames must carry
`translate(-50%)` or the toast jumps to the right edge mid-animation. Gated behind `motion-safe:`; also picked up
`role="status"`/`aria-live` and `mb-[env(safe-area-inset-bottom)]`. Verified: `npm run build` clean, `dotnet test`
green (62/95), and all three variant classes + the emitted keyframe confirmed in the CSS bundle. Click-through
still pending, same as M16.

**v2.1 (M18) — `SongPickerModal` + unified `Tracklist`.** The inline `SongPicker` is gone, replaced by
[`SongPickerModal`](src/Zmg.Web/src/features/catalog/components/SongPickerModal.tsx) on M16's `Modal`:
it **browses on open** (`api.songs.list({ artistId })`, no typing — the "I forgot the title" case) and every
query stays artist-scoped, so another artist's songs can't be linked; typing debounces at 250ms but browse-on-open
is immediate (the delay is `term ? 250 : 0`). No backend change — `artistId` filtering already existed.
[`Tracklist`](src/Zmg.Web/src/features/releases/components/Tracklist.tsx) is now the **one** album tracklist for
both contexts (row + ↑/↓ + ✕, `ml-3` before ✕); `TrackRow.tsx` and its kebab are deleted, and `onToggleFocus` is
**optional** because the focus track only exists once a release is saved. Two adapters keep persistence where it
was: `ReleaseDetailPage` maps `TrackDto[]`→rows keyed by `songId` (a `track(row)` lookup feeds the existing
optimistic `api.tracks.*` handlers), and `TracksEditor` maps its local `EditorRow[]`, putting a new song's
title/ISRC/feats in a per-row **"Details (optional)"** disclosure. Two decisions worth knowing: **the single keeps
its own fixed one-row editor** (it's not a list — nothing to reorder, no add row) and only swaps the inline picker
for the modal; and **`TrackList.tsx` → `Tracklist.tsx` had to be a `git mv`** — macOS is case-insensitive, so the
two names are the same file and writing one silently overwrote the other. Also fixed a regression this milestone
would have introduced: `InlineAddForm`'s buttons had no `type`, harmless on the detail page but inside the create
form's `<form>` they default to `submit` — "+ Add track" would have saved the release. Verified: `npm run build`
clean, `dotnet test` green (62/95), and the app boots (`/` 200) with `GET /api/songs?artistId=…` returning the
artist's songs unfiltered and `&q=` filtering within that scope.

**v2.1 — browser click-through + one fix (post-M18).** The interactive verification outstanding for M16–M18 is
now done in a browser (in-app browser driving `:5274`, dev db seeded with **two** artists so cross-artist scoping
is observable): **Modal** — centered card at desktop (measured rect centre = viewport centre) and a full-width
bottom sheet below `sm`; **Escape** and **backdrop click** both close it and release the body scroll-lock.
**ConfirmDialog** — the catalog delete confirm shows the red `danger` button and deletes end-to-end (song gone
from `/api/songs`). **M17 toast** — the post-save song toast is the green `bg-emerald-600/90` "✓ Saved." with
`role="status"` + the `toast-in` keyframe. **M18 `SongPickerModal`** — browses on open, and with two artists
seeded the picker for Aurora's release lists **only** Aurora's songs (Bruno's excluded, confirming the artist
scope in the UI, not just the API), with `&q=` narrowing within that scope. Cross-artist exclusion is therefore
confirmed at both the API and UI layers. **Fix found in the process:** `Modal`'s `panel.focus()` effect ran after
the child mounted and stole focus back from `SongPickerModal`'s `autoFocus` search input, so "browse on open, just
type" silently didn't — focus landed on the dialog `<div>`. Guarded it (`if (!panel.current?.contains(document.activeElement))`)
so a child that already claimed focus keeps it, while a dialog with no focusable child (ConfirmDialog) still gets
panel focus. Re-verified in the browser: the search input now holds focus on open. `npm run build` + `dotnet test`
green (62/95).

**v2.1 — add-time track details on the release detail page (post-M18).** M18 deliberately kept a new song's
ISRC/feats **create-only** (a per-row "Details (optional)" disclosure in `TracksEditor`), so the detail-page
tracklist's "+ Add track" captured a title only and created the song bare — you then set ISRC/feats on the
catalog song page. That round-trip was friction, so the detail page now collects them at add time. New
[`NewTrackForm`](src/Zmg.Web/src/features/releases/components/NewTrackForm.tsx): a title field with a "Details
(optional)" disclosure (ISRC + the shared `SongArtistsEditor` feats control) that submits the whole song in one
`api.tracks.add` call — the backend already accepted `Isrc`/`Artists` on `TrackInput`
([`TrackService`](src/Zmg.Api/Services/TrackService.cs) → `SongMapping.NewSong`), so **no backend change**.
`Tracklist`'s `onAddNew` is now `(draft: NewTrackDraft) => void` (title + isrc + artists) for both contexts, and it
renders `NewTrackForm` **only when passed the full `artists` list** (the detail page does; the create form doesn't,
so it keeps its title-only `InlineAddForm` + per-row disclosure — that flow is unchanged and was regression-tested).
`ReleaseDetailPage` fetches `api.artists.list()` for the feats control. This is a deliberate, narrow exception to
"song fields are edited only on the catalog page": it's **creation-time** entry from the tracklist, not ongoing
editing of an existing song — release-detail rows still only reorder/focus/remove. Verified in-browser end-to-end:
adding "Duet in the Dark" with an ISRC + a Bruno feat on Aurora's album persisted both (`/api/releases/{id}` shows
the ISRC and the featured artist), the row shows its ISRC, and the create-form album add still works. `npm run
build` + `dotnet test` green (62/95).

**v2.1 — song-title uniqueness per artist + immutable main artist (bugfix round).** Song titles are now
**unique per main artist** (among active/non-archived songs), enforced as a hard rule instead of the old
non-blocking warning — the warning was surfacing in the song **detail** form where there's no release context,
which never made sense. [`Validation.ValidateSong`](src/Zmg.Domain/Validation.cs) now returns an **error**
(`Validation.DuplicateSongTitleMessage`, a shared const) on a clash; enforced everywhere a song is minted:
`SongService` create/rename (400), `ReleaseService.CreateAsync` inline tracks (also catches two identical new
titles in one request), and `TrackService.AddAsync` (the detail-page "+ Add track" path, which previously had
**no** check at all — the reported bug). On the detail page, a duplicate `api.tracks.add` opens a **"Song already
exists" `Modal`** ([`ReleaseDetailPage`](src/Zmg.Web/src/features/releases/ReleaseDetailPage.tsx)) offering *Add
existing song* (looks the clash up via `api.songs.list({ artistId, q })`; hidden when it's already on the release)
or *Change the name* — and `NewTrackForm`'s `onAdd` now resolves `false` to keep the form open with the typed
values. **Main artist is immutable after creation** ([`SongService.UpdateAsync`](src/Zmg.Api/Services/SongService.cs)
409s on a change; a song may already be on that artist's releases) — the detail page renders it as a read-only field.
Dead "Saved with warnings" UI dropped from both song pages (songs no longer emit warnings). Two API-test helpers that
reused a fixed `"Track 1"` title per artist now derive it from the release title. Verified in-browser end-to-end
(both modal branches, read-only main artist). `dotnet test` green (domain 62 / API 99); SPA typechecks + builds.

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
  already supports arbitrary orderings if DnD is added later.
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

- **build-plan-2.1 (M16–M18) — UX refinements. Fully shipped and browser-verified** (M16 `Modal`/confirm,
  M17 toast variants, M18 `SongPickerModal` + unified `Tracklist`) — see the journal entries above.
  - The interactive click-through (sheet vs. card, Escape/backdrop, red-delete confirm, green "Saved." toast,
    picker browse-on-open) is **done** in a browser, and cross-artist picker scoping is **confirmed** at both
    the API and UI layers with a two-artist seed. A focus-steal in `Modal` (defeated `SongPickerModal`'s
    `autoFocus`) was found and fixed in the same pass.
  - Not driven in the browser: the archive-confirm cascade list specifically (needs a release with dormant
    cascading songs) — the underlying `ConfirmDialog`/`Modal` and the `ReactNode` body are otherwise verified.
- **v2.0 is fully shipped (M12–M15).** See the journal above and [build-plan-2.0.md](build-plan-2.0.md).
- **Phase 2 — DSP stats** (the reason this exists over Notion/Trello): hang streaming/revenue data off
  the stable Artist / Release / **Song** / Track ids and UPC/ISRC columns. The v2.0 Song ids are its
  foundation.
- **Per-track task fan-out** on albums: registrations that repeat per track are single "per track"
  tasks today. Decide after the first real album.
- **Verify the Docker image** on a machine with the daemon running (`docker build -t zmg-tracker .`) —
  written and reviewed, never built here (daemon was down).
- Deferred: un-archive/restore and hard-delete/purge (archives are terminal by rule); auth for hosted
  deploys; absolute per-task due dates (v1.1 only added timeframe *ranges*).

**Env note:** `npm run lint` fails locally — oxlint's native binding (`oxlint.darwin-universal.node`)
is missing, same class as the Vite 8 rolldown binding (Vite is pinned to 7.x for that reason). Use
`tsc --noEmit` + `npm run build` to typecheck until it resolves.
