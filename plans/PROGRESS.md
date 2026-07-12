# Progress / Handoff

Snapshot of what's built so far, decisions made during implementation, and what to
do next. Pairs with the versioned build plans — this doc is the "current state" they
can't carry. Read the plans for scope/rationale; read this for where things stand.

**Plan versions:**
- [build-plan-1.0.md](build-plan-1.0.md) — frozen v1 brief (M0–M5). Shipped.
- [build-plan-1.1.md](build-plan-1.1.md) — singles improvements (M6–M10). **All built (M6–M10).**

Newer plan versions live in new `build-plan-N.N.md` files; older ones stay frozen.

**As of:** v1 (M0–M5) done and verified. **v1.1 (M6–M10) complete and verified:** schema/seed foundation;
UPC/ISRC + soft warnings; task timeframes & notes; Home vs All Releases navigation; and the pending-actions
engine. Deferred/next-phase items are in the [Backlog](#backlog-deferred--next-phase-not-gaps-in-v1-or-v11).

---

## What's built (M0 + M1 + M2 + M3 + M4 + M5)

**Backend — `src/Zmg.Domain` (pure, no I/O):**
- Entities + enums (`ReleaseType`, `Phase`, `ArtistRole`) — [Entities.cs](src/Zmg.Domain/Entities.cs), [Enums.cs](src/Zmg.Domain/Enums.cs)
- `TemplateCopy.CopyToRelease` — snapshots a template onto a release ([TemplateCopy.cs](src/Zmg.Domain/TemplateCopy.cs))
- `ProgressCalculator` — done/total overall + per phase ([Progress.cs](src/Zmg.Domain/Progress.cs))
- `ReleaseStatus.Derive` — Upcoming / Released / Complete, derived not stored ([ReleaseStatus.cs](src/Zmg.Domain/ReleaseStatus.cs))
- `Validation` — every §6 rule as a pure function ([Validation.cs](src/Zmg.Domain/Validation.cs))
- `SeedData` — both templates verbatim from §5.4, deterministic ids ([SeedData.cs](src/Zmg.Domain/SeedData.cs))

**Backend — `src/Zmg.Api`:**
- `ZmgDbContext` + EF `HasData` seeding, initial migration ([Data/](src/Zmg.Api/Data), [Migrations/](src/Zmg.Api/Migrations))
- Migrations applied automatically on startup
- Artists CRUD, Releases CRUD (create copies the type's template) ([Endpoints/](src/Zmg.Api/Endpoints))
- Release-task mutations (M2): add / update / toggle / reorder / delete ([Endpoints/TaskEndpoints.cs](src/Zmg.Api/Endpoints/TaskEndpoints.cs))
- Template management (M3): `GET /api/templates`, add / update (rename+move phase) / reorder / delete template tasks; delete blocked on a template's last task (409) ([Endpoints/TemplateEndpoints.cs](src/Zmg.Api/Endpoints/TemplateEndpoints.cs))
- Track CRUD (M4): add / update (rename + focus flag) / toggle focus / reorder / delete, scoped to a release; `TrackNumber` stays 1-based and contiguous (reorder rewrites it, delete renumbers survivors). `ReleaseDetailDto` now carries `Tracks` ([Endpoints/TrackEndpoints.cs](src/Zmg.Api/Endpoints/TrackEndpoints.cs))
- `GET /api/health`; serves the SPA from `wwwroot` with SPA fallback

**Frontend — `src/Zmg.Web` (React + Vite + Tailwind SPA):**
- Dashboard: cards (cover, type/status badges, progress bar, days-to-release), artist/type/status filters, empty states ([pages/Dashboard.tsx](src/Zmg.Web/src/pages/Dashboard.tsx))
- Artists: list + create/edit/delete ([pages/Artists.tsx](src/Zmg.Web/src/pages/Artists.tsx))
- Release form: create/edit per §7.1, template-size hint, inline warnings ([pages/ReleaseForm.tsx](src/Zmg.Web/src/pages/ReleaseForm.tsx))
- Release detail (M2, §8.2): checklist grouped Pre/Release/Post, per-phase progress, done-phases collapsed, one-tap toggle with optimistic update + revert-on-failure toast, `[⋮]` menu (rename / notes / move phase / move up-down / delete), add task; header progress recomputed from the loaded task list, no extra fetch ([pages/ReleaseDetail.tsx](src/Zmg.Web/src/pages/ReleaseDetail.tsx)). Dashboard cards (cover + title) now open the detail.
- Templates (M3, §8 screen 5): tabs Single/Album, "future releases only" banner, per-phase sections with add / rename (inline) / delete / move-phase / move up-down; flat task array recomputed client-side per mutation, no re-fetch; `/templates` route + nav link ([pages/Templates.tsx](src/Zmg.Web/src/pages/Templates.tsx))
- Tracklist (M4): album-only section on the release detail (renders when `type === Album`) — numbered rows, focus-track star (optimistic toggle, gold ★ + "focus" label), inline rename, `[⋮]` menu (rename / set-unset focus / move up-down / delete), add track; local track array renumbered client-side on reorder/delete to mirror the server ([pages/ReleaseDetail.tsx](src/Zmg.Web/src/pages/ReleaseDetail.tsx))
- Typed API client that maps §6 validation errors ([api.ts](src/Zmg.Web/src/api.ts))

**Polish — M5 (§10):**
- **Mobile pass** verified at 375px on every screen: nav wordmark collapses to the badge below `sm` so the links never overflow ([App.tsx](src/Zmg.Web/src/App.tsx)); Artists rows stack (name over count+actions) instead of cramping four items on one line ([Artists.tsx](src/Zmg.Web/src/pages/Artists.tsx)); the release-detail header meta row wraps cleanly (`flex-wrap` + `whitespace-nowrap`) so the date no longer breaks mid-value ([ReleaseDetail.tsx](src/Zmg.Web/src/pages/ReleaseDetail.tsx)); dashboard filters and cards already reflowed to one column. Release detail (the daily-driver screen) keeps big tap targets, cover hidden on phones, optimistic toggles.
- **Filters** (dashboard: artist / type / status + clear) and **empty states** (no releases, no artists, empty phases, empty tracklist, "need an artist first" on the release form) were in from earlier milestones — confirmed present in the mobile pass.
- **Dockerfile** ([Dockerfile](Dockerfile)) + [.dockerignore](.dockerignore): one multi-stage image — `node:22-alpine` builds the SPA (outDir overridden to a local `dist`), `dotnet/sdk:8.0` publishes the API and copies the SPA into `wwwroot`, `dotnet/aspnet:8.0` runs it on `:8080`. SQLite path points at `/data/zmg.db` (`ConnectionStrings__Zmg`) for a mounted volume. Not docker-built locally (daemon wasn't running) — but both wrapped steps (`npm run build`, `dotnet publish`) are verified green, and README documents `docker build`/`docker run`.

**Tests — 60 passing (`dotnet test`):**
- Domain unit (25): template copy (counts, phase/order, IsDone=false, lineage, fresh ids), progress, status, one per validation rule (incl. track-title), seed-data counts
- API integration (35): `WebApplicationFactory` + SQLite in-memory — golden path (artist → release → checklist matches template), artist-delete 409, duplicate-name 400, past-date warning, type filter; **M2 task endpoints (8)**: toggle done/undone + CompletedAt stamp, add appends to phase (+ blank-title 400), update rename/move-phase, reorder persists (+ missing-ids 400), delete lowers total, toggle-missing 404 ([ReleaseTaskApiTests.cs](tests/Zmg.Api.Tests/ReleaseTaskApiTests.cs)); **M3 template endpoints (9)**: list returns both templates with seeded counts, add appends (+ blank-title 400), rename/move-phase, reorder persists (+ missing-ids 400), delete lowers count, delete-missing 404, and **editing a template (add/rename/delete) leaves an existing release's checklist untouched** ([TemplateApiTests.cs](tests/Zmg.Api.Tests/TemplateApiTests.cs)); **M4 track endpoints (10)**: new album has no tracks, add appends with next number + shows in detail (+ blank-title 400), update rename+focus, focus toggle flips, reorder reverses + renumbers contiguously (+ missing-ids 400), delete removes + renumbers survivors, focus-toggle-missing 404 ([TrackApiTests.cs](tests/Zmg.Api.Tests/TrackApiTests.cs)). Template tests use a fresh factory per test (they mutate shared seed data).

**Verified working:** ran the app (API serving the built SPA on :5274), created "Karen Santana" → "Luz", opened the detail screen, toggled tasks (header recomputed `2/31 · 6%`, phase counts updated live), added an ad-hoc Pre task, opened the `[⋮]` menu (all actions present). Single template = 30 tasks (5 Pre / 18 Release / 7 Post), Album = 40. M3: `/templates` renders both tabs (Single 30, Album 40 with 12 Pre), banner + phase sections present; live add returned the task at the next `sortOrder`, delete returned 204 and counts returned to 30/40. **M4:** created an Album "Raíces" (40 tasks, 12 Pre), added 3 tracks (contiguous numbering), focus-toggled a track, reversed the order, deleted the middle track (survivors renumbered 1,2) — all via API; then drove the browser UI: the album-only Tracklist rendered, focus-toggling a star persisted (gold ★ + "focus" label), and add-track appended a new numbered row. Singles show no tracklist.

---

## Key implementation decisions (deltas from / additions to the plan)

- **Template-copy-on-create is already wired** (backend). The plan slots it under M2, but it's pure, tested, and needed for the golden-path test, so releases are created with a full checklist now. **M2 is the checklist UI, not the copy logic.**
- **Vite pinned to 7.x**, not latest. Vite 8's rolldown native binding (`@rolldown/binding-darwin-x64`) fails to install on this machine ([npm/cli#4828](https://github.com/npm/cli/issues/4828)). If you upgrade Node/npm and want Vite 8, retry a clean `npm install` and confirm the binding resolves.
- **Dev ports:** the SPA proxies `/api` → `http://localhost:5274`, which is the API's default launch-profile port. So in dev, run the API with its default profile (`dotnet run --project src/Zmg.Api`). README and this doc both reflect 5274.
- **Enums serialize as integers** (System.Text.Json default). The TS layer mirrors this (`ReleaseType.Single = 0`, etc.). Keep that contract or switch both sides to string enums together.
- **`erasableSyntaxOnly` disabled** in `tsconfig.app.json` so TS `enum`s compile.
- **Status is derived, never stored** — recomputed from tasks + date on every read (§9). No status column.
- Create/update release responses are wrapped as `{ data, warnings }` (`CreatedWithWarnings<T>`) so non-blocking §6 Layer-2 warnings reach the form.
- **Reorder is move-up/move-down, not drag-and-drop** (M2). The `PUT .../tasks/order` endpoint takes the full ordered id list for a phase; the UI computes the new order from a single swap and posts it, staying dependency-free (no DnD library, works on mobile). Swap to real drag later if wanted — the endpoint already supports arbitrary orderings.
- **Task endpoints return the single mutated `ReleaseTaskDto`** (add/update/toggle) or 204 (reorder/delete). The detail screen holds a flat task array and recomputes phase groups + progress client-side, so no re-fetch after a mutation. Moving a task to another phase appends it to the end of the target phase (`SortOrder = max+1`).
- **Tracks use `TrackNumber` (1-based, contiguous) as both identity-order and display number**, not the tasks' `SortOrder` convention. Reorder rewrites `TrackNumber` from the id order; delete renumbers survivors so there are never gaps. The track endpoints aren't hard-scoped to albums (a Single *could* hold tracks) — the plan's "albums only in practice" is enforced only by the UI showing the tracklist for `type === Album`. No new migration: the `Tracks` table has existed since the M0 initial migration.

## Run

```bash
# dev (two terminals)
dotnet run --project src/Zmg.Api                 # API on :5274 (default profile)
cd src/Zmg.Web && npm install && npm run dev     # SPA on :5173, proxies /api → :5274

# prod-style (one process)
cd src/Zmg.Web && npm run build                  # → src/Zmg.Api/wwwroot
dotnet run --project src/Zmg.Api                 # serves SPA + API together

dotnet test                                      # 60 tests
```

DB is `src/Zmg.Api/zmg.db` (git-ignored, recreated from migrations on startup). Delete it to reset.

## File structure

```
src/Zmg.Domain    entities, enums, copy/progress/status/validation, seed  (no I/O)
src/Zmg.Api       minimal API, EF Core + SQLite, migrations, endpoints
src/Zmg.Web       React + Vite + Tailwind SPA
tests/Zmg.Domain.Tests   xUnit unit tests
tests/Zmg.Api.Tests      integration tests (WebApplicationFactory + in-memory SQLite)
```

---

## v1.1 built so far

**M6 — Schema & seed foundation. ✅ Built.**
- Entities: `Release.Upc` / `Release.Isrc`, and `MinDaysBefore` / `MaxDaysBefore` on both `TemplateTask`
  and `ReleaseTask` ([Entities.cs](src/Zmg.Domain/Entities.cs)). One EF migration
  `AddIdentifiersAndTaskTimeframes` ([Migrations/](src/Zmg.Api/Migrations)) adds the columns; existing
  rows get nulls. Migration applies clean (all API integration tests boot via `Migrate()`).
- `TemplateCopy.CopyToRelease` carries the timeframe fields onto each release task ([TemplateCopy.cs](src/Zmg.Domain/TemplateCopy.cs)).
- Seed refactor ([SeedData.cs](src/Zmg.Domain/SeedData.cs)): a shared `BaseTasks` list (the original 30) feeds
  the album template unchanged; the single template is derived from it with "Distribute to DSPs" (7–14 days
  before) inserted as the **3rd** Pre task and "Pitch to Spotify" set to 7–14. Single **30 → 31** (6 Pre /
  18 Release / 7 Post); album still **40**, with no timeframes and no Distribute task. `SeedData.DistributeToDspsTitle`
  is the shared constant M7/M10 key off. The seed's deterministic ids reuse slots on the shifted Pre rows, so
  the migration is `UpdateData` on those + one `InsertData` (no data loss).
- Existing releases are snapshots and are **not** retro-modified — a release created before M6 simply lacks
  the new task, which is correct per the snapshot rule.
- Tests (**+3 net → 63**): domain copy-carries-timeframe, single counts (31, 6/18/7), Distribute is the 3rd
  Pre task at 7/14, Pitch to Spotify at 7/14, album untouched at 40 with all-null timeframes. Updated the
  older count assertions (single 30→31 / delete 29→30) across domain + API tests. Frontend template-size hint 30→31.

**M7 — Release identifiers (UPC/ISRC) & soft warnings. ✅ Built.**
- Domain: `IdentifierState` ([IdentifierState.cs](src/Zmg.Domain/IdentifierState.cs)) — pure, reused by the
  list flag, the detail header, and (later) M10. `IsDistributed(tasks)` keys off the `Distribute to DSPs` task
  being done; `NeedsWarning(distributed, upc, isrc)` fires only once distributed with a blank id; `MissingLabel`
  builds "Missing UPC, ISRC" for M10.
- API: `ReleaseInput` gained optional `Upc`/`Isrc` (trimmed, blank→null, no format check). The list DTO exposes
  `Upc`/`Isrc` + a `NeedsIdentifierWarning` bool (computed in the projection, no extra fetch); the detail DTO
  returns the same three ([ReleaseEndpoints.cs](src/Zmg.Api/Endpoints/ReleaseEndpoints.cs)).
- Past-date backfill: creating a release dated before today auto-checks **only** its "Distribute to DSPs" task
  (stamps `CompletedAt`), so a blank id immediately surfaces the warning (and, later, an M10 pending action).
- Frontend: UPC/ISRC fields on the release form ([ReleaseForm.tsx](src/Zmg.Web/src/pages/ReleaseForm.tsx)); a soft
  amber `IdentifierWarning` glyph ([ui.tsx](src/Zmg.Web/src/ui.tsx)) with a "Missing UPC/ISRC" tooltip, shown on
  dashboard cards and the release-detail header when `needsIdentifierWarning`. (Home/All-Releases split is M9;
  the warning is wired into today's dashboard card until then.)
- Tests (**+8 → 71**): domain `IdentifierState` (5); API UPC/ISRC round-trip on create+update, warning silent
  until distributed / true with blank id / clears when both filled (list flag agrees), past-date auto-check (3).
- Verified against a running API: past-date create → `doneTasks=1`, Distribute checked, warning true; future
  create with ids → round-trips, no warning, total 31.

**M8 — Task timeframes & notes. ✅ Built.**
- API: `AddTaskInput` / `UpdateTaskInput` and `AddTemplateTaskInput` / `UpdateTemplateTaskInput` gained optional
  `MinDaysBefore` / `MaxDaysBefore`; `ReleaseTaskDto` and `TemplateTaskDto` return them. Update is a **full replace**
  of editable fields, so the UI always re-sends the current timeframe (a bare rename won't wipe it) — mirrors how
  Notes already worked ([TaskEndpoints.cs](src/Zmg.Api/Endpoints/TaskEndpoints.cs), [TemplateEndpoints.cs](src/Zmg.Api/Endpoints/TemplateEndpoints.cs)).
- Frontend: `formatTimeframe(min, max)` → "7–14 days before" hint ([ui.tsx](src/Zmg.Web/src/ui.tsx)), shown next to
  Pre-task titles on both the release detail and the template editor. The `[⋮]` menu gains "Set timeframe"
  (Pre-only) opening an inline min–max `TimeframeEditor` (Save / Clear / Cancel). A task with notes shows a "✎"
  indicator next to the title; inline notes editing was already present from M2
  ([ReleaseDetail.tsx](src/Zmg.Web/src/pages/ReleaseDetail.tsx), [Templates.tsx](src/Zmg.Web/src/pages/Templates.tsx)).
- Timeframe is Pre-only in the UI (the "set timeframe" item hides for Release/Post, whose two columns are the
  reserved "days to complete" and drive no logic yet).
- Tests (**+4 → 75**): API round-trip of the timeframe on task add/update (incl. clear-on-omit) and template-task
  add/update. Verified in the running app: seeded Distribute/Pitch tasks copy their 7–14 window onto a new release
  and render the hint; an added task with 3–5 shows "· 3–5 days before".

**M9 — Navigation: Home vs All Releases. ✅ Built.**
- API: the releases list gained `scope` and `q` ([ReleaseEndpoints.cs](src/Zmg.Api/Endpoints/ReleaseEndpoints.cs)).
  `scope=home` filters `releaseDate >= today` and orders **ascending** (nearest-first, forward-looking);
  `scope=all` (default) orders `releaseDate desc`; `q` is a case-insensitive title substring (`EF.Functions.Like`).
- Frontend: the single v1 dashboard split into two pages. **Home** (`/`, [Home.tsx](src/Zmg.Web/src/pages/Home.tsx))
  — forward-looking cards via `scope=home`, artist/type/status filters, New Release, and a slot for the M10
  Pending Tasks section. **All Releases** (`/releases`, [AllReleases.tsx](src/Zmg.Web/src/pages/AllReleases.tsx))
  — a **table** (Name · Type · Released Date · Status) sorted desc, with a debounced title search + artist/type
  filters + New Release; rows link to the detail and carry the M7 soft warning icon. `Dashboard.tsx` removed.
- Nav ([App.tsx](src/Zmg.Web/src/App.tsx)) gained both entries (Home / All Releases) and the `/releases` route.
- Tests (**+3 → 78**): `scope=home` returns only today-or-later; `scope=all` orders desc; title search is a
  case-insensitive substring ([ReleaseListScopeApiTests.cs](tests/Zmg.Api.Tests/ReleaseListScopeApiTests.cs)).
- Verified against a running API: `scope=home` excluded a past-dated release; `scope=all` returned desc; `q`
  matched a single title; the SPA served with both nav entries.

**M10 — Pending-actions engine. ✅ Built.**
- Domain: pure `PendingActions.Compute(release, tasks, today)` in Zmg.Domain
  ([PendingActions.cs](src/Zmg.Domain/PendingActions.cs)), reused by the aggregate endpoint and the detail.
  A `PendingAction` record carries `{ releaseId, releaseTitle, artistName, kind, taskId?, label, daysToRelease? }`
  and a `PendingKind` enum (`TaskDue` / `MissingIdentifier`). *Task due:* an incomplete task with a timeframe
  (keyed off `MaxDaysBefore`, so max drives — generic, not tied to task titles) where the window has opened
  (`today >= releaseDate − MaxDaysBefore`) and it hasn't released yet (`releaseDate >= today`). *Missing
  identifier:* one action per release once "Distribute to DSPs" is done and UPC or ISRC is blank
  (reuses `IdentifierState`). `Order(...)` applies the global ordering: task-due first, nearest release on top;
  data (missing-id) items always last.
- API: `GET /api/pending` ([PendingEndpoints.cs](src/Zmg.Api/Endpoints/PendingEndpoints.cs)) aggregates + orders
  across all releases; the detail DTO gained `pendingActions` (computed in `ToDetail` for that one release).
- Frontend: Home renders the aggregate list as a **Pending Tasks** section (rows link to the release detail),
  filling the M9 slot ([Home.tsx](src/Zmg.Web/src/pages/Home.tsx)); the release detail shows a **Needs attention**
  block at the top from its own `pendingActions`, hidden when empty ([ReleaseDetail.tsx](src/Zmg.Web/src/pages/ReleaseDetail.tsx)).
  Task-due rows show days-to-release; data rows don't.
- Tests (**+8 → 86**): domain `PendingActions` (7 — no-timeframe never pends, window open/closed, completed
  excluded, stops once released, missing-id only after distribution, ordering, empty)
  ([PendingActionsTests.cs](tests/Zmg.Domain.Tests/PendingActionsTests.cs)); API `GET /api/pending` ordering
  across releases (task-due nearest-first, data last) ([PendingApiTests.cs](tests/Zmg.Api.Tests/PendingApiTests.cs)).
- Verified against a running API: a release 3 days out surfaced its Distribute + Pitch-to-Spotify tasks as
  task-due (daysToRelease=3) ahead of a past release's "Missing UPC, ISRC" data action; a release 40 days out
  (window not open) contributed nothing; the detail DTO carried the same `pendingActions`.

**v1.1 (M6–M10) is complete.** Residual/next-phase items are in the backlog below.

---

## Backlog (deferred / next-phase, not gaps in v1 or v1.1)

What follows is the deferred/next-phase backlog beyond M6–M10:

- The tracklist lives on the **release detail** screen, not the release form (the form is create/edit
  of release metadata; tracks are managed after create, like tasks). If a "tracks on the create form"
  flow is wanted later, that's an addition, not a gap.
- **Per-track task fan-out** on albums (§12 open question): registrations repeat per track — v1 keeps
  them as single "per track" tasks. Decide after the first real album.
- **Phase 2 — DSP stats.** The reason this was built instead of using Notion/Trello (§2). Artist /
  Release / Track ids and UPC/ISRC-ready columns are kept stable to hang streaming/revenue data off.
- **Verify the Docker image** on a machine with the daemon running (`docker build -t zmg-tracker .`)
  — it was written and reviewed but not built here (daemon was down).

Reminder that still holds: **editing a template never touches existing releases** (releases own
a snapshot copy) — covered by `TemplateApiTests.Editing_a_template_does_not_touch_existing_releases`.

Env note (unchanged): `npm run lint` currently fails on this machine — oxlint's native binding (`oxlint.darwin-universal.node`) is missing, same class of issue as the Vite 8 rolldown binding. `tsc --noEmit` and `npm run build` both pass, so typecheck through those until the binding resolves.

Known deferrals still open (§12): per-track task fan-out on albums, auth for hosted deploys, and the
phase-2 DSP-stats schema (Artist/Release/Track ids kept stable for it). Task due dates are now partly
addressed by M6's `MinDaysBefore`/`MaxDaysBefore` (a range, Pre-only for pending calc); absolute per-task
due dates remain deferred.
