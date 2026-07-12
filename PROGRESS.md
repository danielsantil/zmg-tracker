# Progress / Handoff

Snapshot of what's built so far, decisions made during implementation, and what to
do next. Pairs with [build-plan.md](build-plan.md) (the full brief) — this doc is the
"current state" the plan can't carry. Read the plan for scope/rationale; read this for
where things actually stand.

**As of:** M0 + M1 complete and verified end-to-end. Next up: **M2 (checklist engine UI)**.

---

## What's built (M0 + M1)

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
- `GET /api/health`; serves the SPA from `wwwroot` with SPA fallback

**Frontend — `src/Zmg.Web` (React + Vite + Tailwind SPA):**
- Dashboard: cards (cover, type/status badges, progress bar, days-to-release), artist/type/status filters, empty states ([pages/Dashboard.tsx](src/Zmg.Web/src/pages/Dashboard.tsx))
- Artists: list + create/edit/delete ([pages/Artists.tsx](src/Zmg.Web/src/pages/Artists.tsx))
- Release form: create/edit per §7.1, template-size hint, inline warnings ([pages/ReleaseForm.tsx](src/Zmg.Web/src/pages/ReleaseForm.tsx))
- Typed API client that maps §6 validation errors ([api.ts](src/Zmg.Web/src/api.ts))

**Tests — 33 passing (`dotnet test`):**
- Domain unit (24): template copy (counts, phase/order, IsDone=false, lineage, fresh ids), progress, status, one per validation rule, seed-data counts
- API integration (9): `WebApplicationFactory` + SQLite in-memory — golden path (artist → release → checklist matches template), artist-delete 409, duplicate-name 400, past-date warning, type filter

**Verified working:** ran the app, created artist "Karen Santana" → release "Luz" → card showed `0/30 done` from the copied Single template. Single template = 30 tasks (5 Pre / 18 Release / 7 Post), Album = 40.

---

## Key implementation decisions (deltas from / additions to the plan)

- **Template-copy-on-create is already wired** (backend). The plan slots it under M2, but it's pure, tested, and needed for the golden-path test, so releases are created with a full checklist now. **M2 is the checklist UI, not the copy logic.**
- **Vite pinned to 7.x**, not latest. Vite 8's rolldown native binding (`@rolldown/binding-darwin-x64`) fails to install on this machine ([npm/cli#4828](https://github.com/npm/cli/issues/4828)). If you upgrade Node/npm and want Vite 8, retry a clean `npm install` and confirm the binding resolves.
- **Dev ports:** the SPA proxies `/api` → `http://localhost:5274`, which is the API's default launch-profile port. So in dev, run the API with its default profile (`dotnet run --project src/Zmg.Api`). README and this doc both reflect 5274.
- **Enums serialize as integers** (System.Text.Json default). The TS layer mirrors this (`ReleaseType.Single = 0`, etc.). Keep that contract or switch both sides to string enums together.
- **`erasableSyntaxOnly` disabled** in `tsconfig.app.json` so TS `enum`s compile.
- **Status is derived, never stored** — recomputed from tasks + date on every read (§9). No status column.
- Create/update release responses are wrapped as `{ data, warnings }` (`CreatedWithWarnings<T>`) so non-blocking §6 Layer-2 warnings reach the form.

## Run

```bash
# dev (two terminals)
dotnet run --project src/Zmg.Api                 # API on :5274 (default profile)
cd src/Zmg.Web && npm install && npm run dev     # SPA on :5173, proxies /api → :5274

# prod-style (one process)
cd src/Zmg.Web && npm run build                  # → src/Zmg.Api/wwwroot
dotnet run --project src/Zmg.Api                 # serves SPA + API together

dotnet test                                      # 33 tests
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

## Next steps (M2 — checklist engine UI, the core deliverable)

Backend endpoints **not yet built** (add under `Endpoints/`, mirror existing style, cover with tests):
- `POST /api/releases/{id}/tasks` — add ad-hoc task (title, phase)
- `PUT /api/tasks/{id}` — rename / move phase / notes
- `PATCH /api/tasks/{id}/toggle` — check/uncheck, stamp `CompletedAt`
- `PUT /api/releases/{id}/tasks/order` — reorder within a phase
- `DELETE /api/tasks/{id}`

Frontend:
- **Release detail screen** (§8.2 wireframe): tasks grouped Pre/Release/Post, per-phase progress, one-tap toggle with **optimistic update + revert-on-failure toast**, `[⋮]` per-task menu (rename/notes/move/delete), add task, collapsed done-phases. Route stubs `/releases/:id` don't exist yet — the dashboard cards currently link to edit only.
- Progress header recomputed from the loaded task list (no extra fetch).
- Priority: fast + big tap targets on mobile — toggling is the daily action.

Then, per the plan's milestones:
- **M3** — Templates screen + template-task CRUD (`GET /api/templates`, etc.)
- **M4** — Album track list (add/reorder, focus-track flag), album template surfaced end to end
- **M5** — mobile polish pass, Dockerfile

Known deferrals still open (§12): per-track task fan-out on albums, task due dates (`dueOffsetDays`), auth for hosted deploys, and the phase-2 DSP-stats schema (Artist/Release/Track ids kept stable for it).
