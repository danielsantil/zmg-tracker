# Progress / Handoff

Snapshot of what's built so far, decisions made during implementation, and what to
do next. Pairs with [build-plan.md](build-plan.md) (the full brief) — this doc is the
"current state" the plan can't carry. Read the plan for scope/rationale; read this for
where things actually stand.

**As of:** M0 + M1 + M2 complete and verified end-to-end. Next up: **M3 (template management)**.

---

## What's built (M0 + M1 + M2)

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
- `GET /api/health`; serves the SPA from `wwwroot` with SPA fallback

**Frontend — `src/Zmg.Web` (React + Vite + Tailwind SPA):**
- Dashboard: cards (cover, type/status badges, progress bar, days-to-release), artist/type/status filters, empty states ([pages/Dashboard.tsx](src/Zmg.Web/src/pages/Dashboard.tsx))
- Artists: list + create/edit/delete ([pages/Artists.tsx](src/Zmg.Web/src/pages/Artists.tsx))
- Release form: create/edit per §7.1, template-size hint, inline warnings ([pages/ReleaseForm.tsx](src/Zmg.Web/src/pages/ReleaseForm.tsx))
- Release detail (M2, §8.2): checklist grouped Pre/Release/Post, per-phase progress, done-phases collapsed, one-tap toggle with optimistic update + revert-on-failure toast, `[⋮]` menu (rename / notes / move phase / move up-down / delete), add task; header progress recomputed from the loaded task list, no extra fetch ([pages/ReleaseDetail.tsx](src/Zmg.Web/src/pages/ReleaseDetail.tsx)). Dashboard cards (cover + title) now open the detail.
- Typed API client that maps §6 validation errors ([api.ts](src/Zmg.Web/src/api.ts))

**Tests — 41 passing (`dotnet test`):**
- Domain unit (24): template copy (counts, phase/order, IsDone=false, lineage, fresh ids), progress, status, one per validation rule, seed-data counts
- API integration (17): `WebApplicationFactory` + SQLite in-memory — golden path (artist → release → checklist matches template), artist-delete 409, duplicate-name 400, past-date warning, type filter; **M2 task endpoints (8)**: toggle done/undone + CompletedAt stamp, add appends to phase (+ blank-title 400), update rename/move-phase, reorder persists (+ missing-ids 400), delete lowers total, toggle-missing 404 ([ReleaseTaskApiTests.cs](tests/Zmg.Api.Tests/ReleaseTaskApiTests.cs))

**Verified working:** ran the app (API serving the built SPA on :5274), created "Karen Santana" → "Luz", opened the detail screen, toggled tasks (header recomputed `2/31 · 6%`, phase counts updated live), added an ad-hoc Pre task, opened the `[⋮]` menu (all actions present). Single template = 30 tasks (5 Pre / 18 Release / 7 Post), Album = 40.

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

## Run

```bash
# dev (two terminals)
dotnet run --project src/Zmg.Api                 # API on :5274 (default profile)
cd src/Zmg.Web && npm install && npm run dev     # SPA on :5173, proxies /api → :5274

# prod-style (one process)
cd src/Zmg.Web && npm run build                  # → src/Zmg.Api/wwwroot
dotnet run --project src/Zmg.Api                 # serves SPA + API together

dotnet test                                      # 41 tests
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

## Next steps (M3 — template management)

Backend endpoints **not yet built** (add under `Endpoints/`, mirror existing style, cover with tests):
- `GET /api/templates` — both templates with their tasks, grouped by phase
- `POST /api/templates/{id}/tasks` — add a template task (title, phase)
- `PUT /api/template-tasks/{id}` — rename / move phase
- `PUT /api/templates/{id}/tasks/order` — reorder within a phase
- `DELETE /api/template-tasks/{id}` — delete, blocked if it's the template's last task (§6: `Validation.ValidateTemplateTaskDelete` already exists, unused so far)

Frontend:
- **Templates screen** (§8 screen 5): tabs Single/Album, add/rename/delete/reorder tasks, move between phases. Banner: "changes apply to future releases only". Add a `/templates` route + nav link.
- Reuse the M2 task-row/phase-section patterns from [ReleaseDetail.tsx](src/Zmg.Web/src/pages/ReleaseDetail.tsx) where sensible (no checkbox/toggle on template tasks).

Reminder that must hold: **editing a template never touches existing releases** (releases own a snapshot copy). There's an integration test asserting this for the copy path; add one for template edits if not covered.

Then, per the plan's milestones:
- **M4** — Album track list (add/reorder, focus-track flag), album template surfaced end to end
- **M5** — mobile polish pass, Dockerfile

Env note (unchanged): `npm run lint` currently fails on this machine — oxlint's native binding (`oxlint.darwin-universal.node`) is missing, same class of issue as the Vite 8 rolldown binding. `tsc --noEmit` and `npm run build` both pass, so typecheck through those until the binding resolves.

Known deferrals still open (§12): per-track task fan-out on albums, task due dates (`dueOffsetDays`), auth for hosted deploys, and the phase-2 DSP-stats schema (Artist/Release/Track ids kept stable for it).
