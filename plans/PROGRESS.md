# Progress / Handoff

A running journal: what shipped per version, decisions that *aren't* captured in a build plan,
and what's next. Read the **build plans** for scope, rationale, wireframes, and per-milestone test
lists; read **this** for current state and the cross-cutting knowledge the plans can't carry.

**Plan versions**
- [build-plan-1.0.md](build-plan-1.0.md) — frozen v1 brief (M0–M5). Shipped.
- [build-plan-1.1.md](build-plan-1.1.md) — singles improvements (M6–M10). Shipped.
- [build-plan-1.2.md](build-plan-1.2.md) — archived releases (M11). Shipped.
- [build-plan-2.0.md](build-plan-2.0.md) — songs & catalog (M12–M15). **M12–M15 shipped — v2.0 complete.**
- [build-plan-2.1.md](build-plan-2.1.md) — UX refinements (M16–M18). **M16 shipped; M17–M18 next** — see [Backlog](#backlog--next-steps).

Newer plan versions go in new `build-plan-N.N.md` files; older ones stay frozen.

**Current state:** **build-plan-2.0 fully shipped** (M12–M15) — songs, catalog, pending rework, and the
archive cascade — plus a round of post-v2.0 UX/feature improvements, and now **M16** of
[build-plan-2.1](build-plan-2.1.md) (the `Modal`/confirm-dialog primitives). Tests green (`dotnet test` —
domain 62 / API 95). Next work is **M17 (toast variants) → M18 (`SongPickerModal` + unified `Tracklist`)**
(see [Backlog / next steps](#backlog--next-steps)); Phase 2 (DSP stats) follows.

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
  with 409. A single is fixed at one track; an album has zero+. Song fields are edited only on the
  catalog detail page (M13) — release-detail track rows just reorder/focus/remove.
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

- **build-plan-2.1 (M16–M18) — UX refinements. M16 shipped; M17 → M18 next.**
  - **M16 — shared `Modal` primitive + custom confirm dialog. Shipped** — see the journal entry above.
    Outstanding: the interactive click-through from the plan's Verification list (bottom sheet vs.
    centered card, Escape/backdrop dismiss, cascade list in the archive confirm).
  - **M17 — Toast variants.** `Toast`/`useToast` gain `variant: 'success' | 'error' | 'info'` (default
    `error` so existing callers keep red). The post-save "Saved." in `SongDetailPage` becomes green
    success — fixing the current red-pop-up-that-looks-like-failure.
  - **M18 — `SongPickerModal` + unified `Tracklist`.** Replace the inline
    [`SongPicker`](src/Zmg.Web/src/features/catalog/components/SongPicker.tsx) with a `Modal`-based
    picker that **browses the release's main-artist songs on open** (no typing needed) and stays
    artist-scoped (`api.songs.list({ artistId })` — already supported, no backend change). Unify the
    create-form (`TracksEditor`) and detail-page (`TrackList`) tracklists into one `Tracklist`
    component with a single row design and standard ↑/↓ reorder (retire the detail-page kebab menu).
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
