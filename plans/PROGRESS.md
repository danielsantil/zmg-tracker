# Progress / Handoff

Current state, what shipped per version, and the cross-cutting knowledge no single build plan carries.
Read the **build plans** for scope, rationale, wireframes, and per-milestone test lists; read **this**
for where the project stands and the rules that span plans.

**Plan versions**
- [build-plan-1.0.md](build-plan-1.0.md) — frozen v1 brief (M0–M5). Shipped.
- [build-plan-1.1.md](build-plan-1.1.md) — singles improvements (M6–M10). Shipped.
- [build-plan-1.2.md](build-plan-1.2.md) — archived releases (M11). Shipped.
- [build-plan-2.0.md](build-plan-2.0.md) — songs & catalog (M12–M15). Shipped.
- [build-plan-2.1.md](build-plan-2.1.md) — UX refinements (M16–M18). Shipped.
- [build-plan-2.2.md](build-plan-2.2.md) — UX improvements (M19–M23). Shipped.
- [build-plan-2.3.md](build-plan-2.3.md) — refactor · code health (M24–M25). **M24 (web) + M25 (API +
  defects + the test-hygiene sweep) shipped. Only a live `docker build` verify is left (see backlog).**

Newer plan versions go in new `build-plan-N.N.md` files; older ones stay frozen.

**Current state:** feature-complete through v2.2. **v2.3 M24 (web) and M25 (API + defects) are shipped.**
M25 closed the archived-release write gap, hoisted the title-clash rule, added `AsNoTracking`/query
tidy, shipped `canArchive` on the release DTOs, threaded `CancellationToken`, split
`ReleaseService.CreateAsync`, fixed the Dockerfile + a fail-fast connection-string guard, and defused the
test date bomb. The **test-hygiene sweep** then landed (Domain ObjectMother + AAA; `SongArchiveApiTests`
13→1 and `ReleaseArchiveApiTests` 8→1 host boots via `IClassFixture`; redundant integration tests
deleted). Backend tests **domain 73 / API 136**, green (~6s). The **two M24 web items** are closed too —
the SPA now reads `canArchive` from the DTO and matches the mirrored duplicate-title constant. The SPA
has **28 Vitest** tests. **One thing left before Phase 2:** a live `docker build` verify (the Dockerfile
fix is unverified — the daemon was down). Then **Phase 2 — DSP stats** (no build plan yet).

> ⚠️ **v2.0's `InitialCreate` is a hard schema reset with no migration path.** Any local
> `src/Zmg.Api/zmg.db` from v1.x must be deleted, not upgraded (`rm src/Zmg.Api/zmg.db*`) — startup
> recreates a seeded db.

---

## Journal

**v1 (M0–M5) — foundation.** Domain (entities, template-copy, progress, derived status, validation,
seed), the minimal API + EF/SQLite with seeded templates, and the React SPA (dashboard, artists,
release form + detail checklist, templates editor, album tracklist). M5 was polish: 375px mobile pass,
filters, empty states, and the multi-stage Dockerfile.

**v1.1 (M6–M10) — singles improvements.** UPC/ISRC + the soft "missing identifier" warning; per-task
timeframes (Pre-only, max drives the calc, the range is display-only); the dashboard split into
**Home** (forward-looking) and **All Releases**; and the **pending-actions** engine (`GET /api/pending`
+ the detail "Needs attention" block). The single template grew 30 → **31** (Distribute inserted as 3rd
Pre); album stayed at **40**.

**v1.2 (M11) — archived lifecycle.** An `Archived` status plus a soft-delete: `ArchivedAt`/`DeletedAt`
on Release, `POST /api/releases/{id}/archive` (guarded to `releaseDate >= today`), `DELETE` repurposed
as a guarded soft-delete (archived only), a `/releases/archived` page, and a read-only archived detail.

**v2.0 (M12–M15) — songs & catalog.** Split a first-class **Song** (title, main artist, ISRC,
feats/collabs, own archive lifecycle) from **Release** (UPC, cover, tasks), linked by a pure `Track`
join so one song can sit on a single *and* an album. Added the **Catalog** (list + detail with a
derived release date/UPC), reworked pending actions, and made archiving a release cascade to the songs
exclusive to it. Shipped as a hard schema reset — all v1.x migrations dropped for one `InitialCreate`.

**v2.1 (M16–M18) — UX refinements.** The `Modal` / `useConfirm` / `ConfirmDialog` primitives (which
retired native dialogs app-wide), toast variants, `SongPickerModal`, and one unified `Tracklist` serving
both the create form and the release detail. Plus an integrity round: per-artist song-title uniqueness
and an immutable song main artist.

**v2.2 (M19–M23) — UX improvements.** Artists redesign (real table, up-front smart delete, dedicated
create/edit pages); `RowMenu` kebabs standardized across the tables and cards; a compact `ReleaseCard`;
the releases **calendar** view (dependency-free month grid + a day preview modal); and one shared inline
↑/↓ reorder control app-wide. Browser verification surfaced four latent bugs worth knowing about — the
artist delete guard, popovers inside modals, mobile table clipping, and page-level horizontal overflow —
each now carried as a rule in Cross-cutting decisions rather than as a story here.

**v2.3 M24 (web refactor) — no features, code health.** `strict` on across the SPA (with the
reachable-null `!`s narrowed); **Vitest** added with 28 tests on the pure modules (`lib/calendar`,
`lib/format`, `usePersistedState`'s extracted core). **TanStack Query** adopted over the unchanged
`api/` modules (`api/queries.ts` — key factory + hooks); a 60s `staleTime` means the artist roster
loads once across navigation instead of on every page. Extracted the list-page shell (`DataTable`,
`EmptyState`, `ErrorBanner`, `Loading`, `FilterBar`, `useConfirmDelete`) and one `useDebouncedValue`
(retiring the 4 copy-pasted debounces + their `exhaustive-deps` disables and the 39 `errorMessage`
repeats). Collapsed the `Template*` fork into generic `TaskRow`/`PhaseSection`/`TimeframeEditor`/
`MovePhaseItems`. Split `ReleaseDetailPage` into `useReleaseTasks`/`useReleaseTracks` (an outer loader
+ inner view over a guaranteed release); `ReleaseFormPage` fields → one `useReducer`. Defects fixed:
`todayIso()` now builds a local date (was UTC), and the stale `TEMPLATE_TASK_COUNT` is gone for a live
`/api/templates` count. cva for the variant maps (which also fixed `Button` dropping a passed
`className`); type-aware ESLint with `no-floating-promises`. **Two items parked on M25** (kept working
in the meantime): the release `canArchive` is still re-derived client-side, and the add-track
duplicate-title branch still string-matches the validator message.

**v2.3 M25 (API + defects) — no features, code health.** Closed all four defects. **(1) Archived-release
write gap:** new pure `ReleaseMutability` (a `CanEdit` 409 rule) now gates `ReleaseService.UpdateAsync`,
all four `TrackService` writes, and all `ReleaseTaskService` writes — the read side already treated
archived as terminal; no write path did. **(2) Dockerfile:** stage 2 now copies `Zmg.Infra` (the
missing project reference that made `dotnet restore` fail), and `Program.cs` fail-fasts on a null `Zmg`
connection string instead of passing it to `UseSqlite` (`ZmgApiFactory` supplies a test value). *The
image build itself still needs a machine with the Docker daemon running — unverified here.* **(3) Stale
template constants:** handled on the web side in M24; **(4) test date bomb:** the ~14
`new DateOnly(2026, 8, 14)` literals → relative `TestDates.Upcoming`, and the four divergent Domain-test
"today"s → one `TestDates.Today`. Also: the inline-song **title-clash rule** (three copies, two
divergent) hoisted into `Validation.ValidateReleaseTracks` (now takes the artist's active titles and
folds the within-request dedupe), so both services call Domain; **`canArchive`** derived server-side via
new pure `ReleaseArchival` and shipped on `ReleaseListItemDto`/`ReleaseDetailDto`; **`AsNoTracking`** on
the read paths (`AsNoTrackingWithIdentityResolution` where an include path cycles), a shared
`SongQueryExtensions.WithDetailIncludes`, `ArtistService.UpdateAsync`'s three `CountAsync` collapsed to
one projection; **`CancellationToken`** threaded through every service + interface + endpoint;
`CreateAsync` decomposed into validate/resolve/build/materialise steps. **Tests:** new unit coverage —
`ReleaseMutabilityTests`, `ReleaseArchivalTests`, `ReorderTests` (via `InternalsVisibleTo`),
`OperationResultExtensionsTests` (status-code mapping), a 22-route **404 Theory**
(`NotFoundRoutesApiTests`), the archived-write **409** suite (`ArchivedReleaseWriteApiTests`), and the
first `?status=` filter + `CanArchive` DTO assertions. Some AAA/Theory tidy landed
(`ReleaseStatusTests`, `ReleaseTests`, `SeedDataTests`, `ValidationTests`). **Deferred (behavior-neutral,
suite green without it):** the shared-fixture/one-lifecycle consolidation to cut host boots, the
exhaustive AAA/Theory pass, and the redundant-integration-test deletions.

---

## Cross-cutting decisions (not in any single plan)

- **Status is derived, never stored** — recomputed from tasks + date on every read. `Archived` (v1.2) is
  the one persisted flag that overrides the derived value.
- **Archived is terminal on *writes* too, not just reads (M25).** The read side always treated archived
  as read-only (`ReleaseWarnings`, `PendingService`), but every write path — release PUT, task edits,
  track edits — silently succeeded. Pure `ReleaseMutability.CanEdit` now gates them all with a **409**,
  matching the song lifecycle. Any new release-write endpoint must call it. The mirror question "may this
  still be archived?" is the separate pure `ReleaseArchival.CanArchive` (upcoming and not yet archived),
  shipped on the release DTOs so the SPA never re-derives `releaseDate >= today`.
- **Soft-delete, never hard-delete** (v1.2). Removed releases are stamped `DeletedAt` and hidden by a
  global query filter, so stable ids survive for phase-2 stats. A join between two soft-filtered entities
  needs **its own** filter (`Track` checks both parents) or a stale join outlives them; EF's "required end
  of a relationship" advisory on the child navs is benign.
- **Template-copy-on-create is backend logic** — a release is born with a full snapshot checklist, and
  editing a template never touches existing releases (locked by `TemplateApiTests`).
- **Reorder is move-up/move-down, not drag-and-drop.** The endpoint takes the full ordered id list for a
  phase; the UI posts a single-swap result (it already supports arbitrary orderings if DnD ever lands).
  Its one control is inline ↑/↓ via `components/ReorderArrows.tsx` — never a kebab item, never a second
  copy of the arrow markup.
- **Mutations return the single changed DTO** (or 204); the detail screen holds a flat task array and
  recomputes phase groups + progress client-side, so no re-fetch. Moving a task across phases appends to
  the target (`SortOrder = max+1`).
- **Tracks key off `TrackNumber`** (1-based, contiguous) for order and display; reorder rewrites it,
  delete renumbers survivors. Tracklist is UI-gated to albums (endpoints aren't hard-scoped).
- **Two warning channels — don't add a third.** Release advisories are one `warnings: string[]` built by
  pure `ReleaseWarnings.Compute` and rendered by a single `SoftWarning` icon; add a new advisory *there*,
  never as another DTO boolean. Create/update **validation** warnings are separate, riding
  `{ data, warnings }` (`CreatedWithWarnings<T>`) so they reach the form.
- **Song vs Release (v2.0).** Song = the creative work (title, ISRC, feats/collabs, main artist); Release
  = the commercial package (UPC, cover, tasks); they meet at `Track`. A song's **UPCs and release date are
  derived** from its links, never stored. **Type is fixed at create** (it picks the checklist) and PUT
  409s on a change; a single is fixed at one track, an album has zero+. Existing songs are edited only on
  the catalog detail page — the exception is **creation**, where a new song may set title/ISRC/feats at
  add time (that's its birth, not later editing).
- **Song titles are unique per main artist; a song's main artist is immutable (v2.1).** Uniqueness is a
  **hard error** in pure `Validation.ValidateSong`, enforced at *every* mint path (song create/rename,
  release create with inline tracks, track add) — never a soft warning. `SongService.UpdateAsync` 409s on
  a main-artist change, since the song may already sit on that artist's releases.
- **Delete guards must count every reference, not just the obvious ones** (v2.2). Counting only
  main-artist links let a feat-only artist past the guard and into a Restrict FK — a 500 where a clean 409
  belonged. Surface the counts in the DTO so the UI can block up front instead of apologising afterward.
- **No native dialogs (M16).** `window.confirm`/`alert` are banned app-wide: ask via `useConfirm()` (one
  `<ConfirmDialog>` under the root provider), report failures with an error toast, and build overlays on
  `components/Modal.tsx` rather than hand-rolling a backdrop. Destructive intent is colour-coded: red
  `danger` for hard deletes, amber `archive` for archiving (terminal ≠ destructive).
- **Popovers positioned from a trigger rect must portal to `<body>` (v2.2).** `position: fixed` resolves
  against a *transformed* ancestor rather than the viewport — inside `Modal` (whose panel is
  `-translate-x/y-1/2`) an in-place popover lands off-panel, gets clipped by the panel's overflow, and
  hides under the backdrop, unclickable. `RowMenu`/`SoftWarning` portal out and sit at `z-50` to clear the
  modal's `z-40`. Anything new that positions this way inherits the trap.
- **The page body never scrolls sideways.** Wide content scrolls inside its own `overflow-x-auto`
  container (every table wrapper); the nav wraps instead of scrolling, because a scrollable nav hides
  destinations behind an affordance nobody discovers. A stray page-level horizontal scroll also closes
  every `RowMenu`, which dismisses on *any* scroll event.
- **Dates are `yyyy-MM-dd` strings — never `new Date('yyyy-MM-dd')`**, which parses as UTC and drifts a
  day back in negative offsets. Compare and group by the raw string; parse at local midnight
  (`+ 'T00:00:00'`) only to format. The calendar builds its cells by hand for this reason, and emits only
  the weeks a month actually touches (4–6) so no all-foreign week appears.
- **UI preferences persist via `usePersistedState`** (v2.2) — `localStorage`, `zmg.`-prefixed keys. Every
  access is try/catch'd (it throws in Safari private mode and wherever site data is blocked; a preference
  is never worth taking the page down for) and validated on read, so a stale key can't load as state the
  UI can't render.
- **One tracklist, two adapters (M18).** `Tracklist.tsx` owns the album row design and controls for both
  the create form and the release detail; neither gets its own row markup. It holds no persistence —
  `TracksEditor` (local rows) and `ReleaseDetailPage` (optimistic `api.tracks.*`) adapt to it. Singles sit
  outside it deliberately: one fixed row, nothing to reorder. Linking an existing song always goes through
  `SongPickerModal`, **always scoped to the release's main artist** — never widen it to the whole catalog.
- **Enums serialize as integers** (System.Text.Json default) and the TS layer mirrors them — change both
  sides together. App code must keep `erasableSyntaxOnly` off or the TS `enum`s stop compiling; it's on
  only in `tsconfig.node.json`, which covers the Vite config rather than `src/`.
- **Buttons inside a `<form>` need an explicit `type`** — HTML defaults to `submit`. Shared components
  that might render inside a form set `type="button"`; `Button` has no default, so real submits stay
  explicit.
- **macOS is case-insensitive — `Foo.tsx` and `foo.tsx` are one file.** Use `git mv` for case-only
  renames; writing the "new" file just overwrites the old one.
- **EF tooling must match the runtime (EF 8).** Nothing is pinned in-repo, but a 10.x-generated migration
  builds fine and then **silently fails at runtime** (`no such table: __EFMigrationsHistory`). Install
  matching tooling before regenerating one.

---

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

- **Shipped — v2.3 M24 (web refactor):** strict + Vitest · TanStack Query · shared list-page shell ·
  `Template*` fork collapsed · `ReleaseDetailPage`/`ReleaseFormPage` split · cva + typed-lint.
- **Shipped — v2.3 M25 (API + defects):** all four defects closed (archived write gap → `ReleaseMutability`
  409s; Dockerfile Infra copy + connection-string fail-fast; stale template constants done in M24; date
  bomb → `TestDates`); title-clash rule hoisted; `canArchive` on the release DTOs (`ReleaseArchival`);
  `AsNoTracking` read paths + `SongQueryExtensions`; `CancellationToken` everywhere; `CreateAsync` split.
  Tests **domain 73 / API 143**, all green. New: `ReleaseMutability`/`ReleaseArchival`/`Reorder`/
  `OperationResultExtensions` units, a 22-route 404 Theory, the archived-write 409 suite, `?status=` +
  `CanArchive` DTO assertions.
- **Shipped — v2.3 M25 test-hygiene sweep:** Domain `Builders.cs` ObjectMother (dedupes the 6 private
  builders) + AAA on `SongArchivalTests`; `SongArchiveApiTests` (13→1 boots) and `ReleaseArchiveApiTests`
  (8→1) moved to `IClassFixture`; redundant integration tests deleted (2 domain-covered cascade cases, 3
  triple-covered pending cases, 2 of the 3 `Reorder_with_missing_ids` now that `Reorder` is unit-tested).
  Tests **domain 73 / API 136**, green; suite wall-clock ~8s → ~6s. `ReleaseListScopeApiTests` (exact
  global ordering) and `PendingApiTests`/`TemplateApiTests` (aggregate reads / template mutation) stay
  per-test isolated by necessity, so boots landed ~29 not the plan's optimistic ~11.
- **Still open (not gating):** **verify `docker build` on a live daemon** (the Dockerfile fix is unverified
  — daemon was down here); and the remaining low-value polish (exhaustive AAA-comment pass on every file,
  the last few Theory conversions). Pure hygiene; the suite is green without it.
- **The two M24 web items M25 unblocked (small SPA follow-up):** the SPA still re-derives `canArchive`
  (`ReleaseDetailPage`/`AllReleasesPage`/`ReleaseCard`) — consume the DTO's new `CanArchive` — and the
  add-track branch still string-matches; mirror `Validation.DuplicateSongTitleMessage` in a TS constant.
- **Then — Phase 2: DSP stats** (the reason this exists over Notion/Trello): hang streaming/revenue data
  off the stable Artist / Release / **Song** / Track ids and the UPC/ISRC columns; the v2.0 Song ids are
  its foundation. No build plan yet — write `build-plan-3.0.md` when it starts.
- **Per-track task fan-out** on albums: registrations that repeat per track are single "per track" tasks
  today. Decide after the first real album.
- **Verify the Docker image** on a machine with the daemon running (`docker build -t zmg-tracker .`) —
  folded into M25 (defect 2); still needs a machine with the daemon running.
- Deferred: un-archive/restore and hard-delete/purge (archives are terminal by rule); auth for hosted
  deploys; absolute per-task due dates (v1.1 only added timeframe *ranges*). Also carried forward from
  the M24 audit: the **seed-data 3-way drift hazard** (`SeedData.cs` → `InitialCreate` → snapshot, with
  `DeterministicTaskId` renumbering every later GUID on a mid-list insert) — left as-is per CLAUDE.md's
  hard-reset rule, noted here so M25/Phase 2 don't rediscover it.
