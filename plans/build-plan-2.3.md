# ZMG Release Tracker — Build Plan v2.3 (refactor · code health)

Delta on top of [build-plan-2.2.md](build-plan-2.2.md). Continues milestone numbering from M23 → **M24–M25**.

## Context

v2.0–v2.2 shipped features fast. This version ships **no features** — it pays down the debt that
accumulated, so Phase 2 (DSP stats) lands on a base that grows without adding more.

An audit of all four projects found the codebase is **not in bad shape**. The `api/` layer, the
`components/` primitives, the confirm/toast/modal infrastructure, `lib/format`+`lib/calendar`, the
minimal-API endpoint layer (one-liners, zero `if` statements), `OperationResult`, and the pure-Domain
design are well-factored and are **left alone**. The problems are concentrated and specific.

**Four real defects, not smells** (each verified against source):

1. **Archived releases are fully mutable.** Every *read* path treats archived as terminal and read-only
   (`ReleaseWarnings.cs:22` — *"Archived releases are terminal and read-only"*, `PendingService.cs:53`,
   `SongService.cs:109`). No *write* path enforces it: `ReleaseService.UpdateAsync`
   (`ReleaseService.cs:205-239`) has no `IsArchived` check, and `TrackService` / `ReleaseTaskService`
   never load release archived state at all. `PUT` on an archived release succeeds today. No test covers it.
2. **`docker build` cannot succeed.** `Dockerfile:21-23` copies only `Zmg.Domain` + `Zmg.Api`, but
   `Zmg.Api.csproj:5` references `..\Zmg.Infra\`, so `dotnet restore` fails. The Dockerfile (last touched
   in `732667d`, "M5 polish… v1 complete") predates the Infra extraction (`fbe6591`, M12). PROGRESS.md's
   *"never actually built"* backlog note corroborates.
3. **Stale template constants.** `ReleaseFormPage.tsx:13-16` hardcodes `{Single: 31, Album: 41}` under a
   comment reading *"A template endpoint arrives in M3; until then these drive the hint."* It arrived —
   `TemplatesPage.tsx:161` renders the real count from `/api/templates`, and the Templates page lets users
   **edit** templates. The hint lies the moment anyone does.
4. **Test date bomb.** ~14 sites hardcode `new DateOnly(2026, 8, 14)`. The API auto-checks
   "Distribute to DSPs" on past-dated releases (proven by `ReleaseIdentifierApiTests.cs:81-95`), so
   `ArtistReleaseApiTests.cs:104` (`Assert.Equal(0, detail.DoneTasks)`) and
   `ReleaseIdentifierApiTests.cs:57-58` **start failing on 2026-08-15**. Four other files already use
   `DateOnly.FromDateTime(DateTime.UtcNow)` correctly.

**Two structural risks on the web side.** `strict` is absent from every tsconfig (verified via
`tsc --showConfig`) — so `strictNullChecks` is off, every `string | null` in `types/` is decorative, and
the 11 `!` assertions in `ReleaseDetailPage` suppress nothing because nothing is being checked. And the
SPA has **zero tests** (no `test` script, no Vitest). Both are fixed *before* code moves — otherwise the
refactor is unverified code moving with no net.

**The duplication** is concentrated in exactly two places: the list pages (5× table, 8× empty state,
12× error banner, 13× loading) and the `Template*` fork (4 component pairs that are near-verbatim copies
of their release counterparts).

**Decisions locked with the user:** adopt TanStack Query; `strict` + Vitest on pure modules (not full
component testing); fix all four defects here; API scope = correctness + queries, **including** splitting
`ReleaseService.CreateAsync`. Explicitly **rejected**: Scrutor/DI auto-registration — 14 explicit,
greppable `AddScoped`/`MapXEndpoints` lines beat reflection at 7 services; and FluentAssertions — the
suite is already 100% consistent plain xunit `Assert.*`, which is a strength, not a migration target.

---

## M24 — Web refactor

**Order is load-bearing.** Tasks 1–2 build the net; nothing moves before they're green.

### 1. Enable `strict` and fix the fallout
- `tsconfig.app.json`: add `"strict": true` beside the existing `/* Linting */` block (`noUnusedLocals`,
  `noUnusedParameters`, `noFallthroughCasesInSwitch`).
- Fix every `npm run build` error. Expect the bulk in `ReleaseDetailPage.tsx` (11 `!` at
  `:104,159,186,191,217,222,228,241,268,288,397`), `ReleaseFormPage.tsx:52-54`, `SongDetailPage.tsx:38`.
- **Replace `!` with real narrowing** where the null is reachable; keep `!` only where an invariant
  genuinely holds, with a comment naming the invariant.
- **Commit this alone.** Every later task is then type-checked.

### 2. Add Vitest + tests for the pure modules
- Dev deps `vitest` + `@vitest/coverage-v8`; scripts `"test": "vitest run"`, `"test:watch": "vitest"`.
  No Testing Library — component tests are out of scope this round.
- Cover: `lib/calendar.ts` (`monthGrid` week-count logic, the `new Date(y, m, 0)` overflow trick,
  adjacent-month fill), `lib/format.ts` (`formatCountdown`, `formatTimeframe`, `daysToRelease`, and
  `todayIso`'s timezone behaviour — see task 4), `hooks/usePersistedState.ts` (the `isValid` guard
  rejecting stale keys, try/catch on both read and write).
- AAA structure, matching the convention adopted in M25.

### 3. Adopt TanStack Query
- Add `@tanstack/react-query`; `QueryClientProvider` in `App.tsx` beside `ConfirmProvider` (`App.tsx:52`).
- New `api/queries.ts`: query-key factory + `useArtists()`, `useReleases(filters)`, `useSongs(…)`,
  `useTemplates()`, `useRelease(id)`, `useSong(id)`, `usePending()`. **The existing `api/` modules stay
  exactly as they are** — already the right shape (thin typed fns over `client.ts`); the hooks wrap them.
- **`useArtists()` is the headline win**: one cached query replaces 8 independent `api.artists.list()`
  fetches (`HomePage.tsx:39`, `ReleaseDetailPage.tsx:71`, `ReleaseFormPage.tsx:45`,
  `AllReleasesPage.tsx:49`, `CatalogPage.tsx:36`, `SongDetailPage.tsx:46`, `SongFormPage.tsx:31`,
  `ArtistsPage.tsx:25`) — the roster is currently refetched on every navigation.
- Mutations via `useMutation` + `invalidateQueries`, replacing the hand-rolled optimistic-update-then-revert
  in `ReleaseDetailPage` and `TemplatesPage`.
- One `useDebouncedValue` hook feeding the query key replaces the 3 copy-pasted debounce blocks
  (`HomePage.tsx:52-55`, `AllReleasesPage.tsx:53-57`, `CatalogPage.tsx:39-43`) and the 4th at
  `SongPickerModal.tsx:43-52`. **This removes all 3 `eslint-disable react-hooks/exhaustive-deps` in the repo.**
- Centralize the **39** `e instanceof ApiError ? e.message : '<fallback>'` repeats into one
  `errorMessage(e, fallback)` in `api/client.ts`.
- Retires 13 hand-rolled `loading` states and 8 `error` states, and the 3 competing fetch idioms
  (`try/catch/finally`, `.then().catch().finally()`, fire-and-forget `.then(setX).catch(() => setX([]))`).

### 4. Fix the defects and pull domain logic back to the server
- **Delete `TEMPLATE_TASK_COUNT`** (`ReleaseFormPage.tsx:13-16`) and its stale comment (`:11-12`); drive
  the hint at `:200` off `useTemplates()`.
- **Fix `todayIso()`** (`lib/format.ts:14-16`): `new Date().toISOString()` is **UTC** — in a negative-offset
  timezone after 00:00 UTC it returns *tomorrow*, so the Archive affordance can appear a day off from what
  the server accepts. Build `yyyy-MM-dd` from local parts, as `lib/calendar.ts:3-5` already documents. Test it.
- **Consume `canArchive` from the server** (shipped by M25) instead of re-deriving `releaseDate >= today`
  at `ReleaseDetailPage.tsx:312`, `AllReleasesPage.tsx:207`, `ReleaseCard.tsx:61`. This matches the pattern
  the codebase **already established** for songs (`SongListItem.canArchive`, `types/song.ts:25`).
- **Drop the error-string parsing** at `ReleaseDetailPage.tsx:202` —
  `e.errors.some(m => m.includes('already exists for this artist'))` is a string-match contract against the
  C# validator. Match on `Validation.DuplicateSongTitleMessage` (M25 task 2) instead.
- **Use the server's per-phase counts:** `ReleaseDetailPage.tsx:44` flattens `detail.phases.flatMap(p => p.tasks)`,
  discarding `PhaseGroup.done`/`total` (`types/task.ts:16-18`), which `PhaseSection.tsx:27-29` then recomputes
  (duplicating `Progress.cs:32-43`). Keep the flat list for optimistic toggles — legitimate and documented at
  `TaskEndpoints.cs:9` — but stop recomputing what already arrived. Note `ProgressBar.tsx:16` re-implements
  `Progress.Percent`; keep one source of truth.
- Leave `ArtistsPage.tsx:36-50` (delete-guard pre-check) and `ReleaseFormPage.tsx:89-99` (track-count
  mirror) as-is — both are deliberate, documented UX choices with the server guard kept as a safety net.

### 5. Collapse the `Template*` fork
Four near-verbatim pairs — generic-ize against the shared shape rather than forking further:

| Release | Template | Action |
|---|---|---|
| `releases/components/MovePhaseItems.tsx` (30) | `templates/components/TemplateMovePhaseItems.tsx` (30) | **100% identical logic** — one `<T extends { phase: Phase }>` in `components/` |
| `releases/components/TimeframeEditor.tsx` (52) | `TemplateTimeframeEditor.tsx` (49) | identical but for the type name + one `pl-12` — one component, padding via prop |
| `releases/components/PhaseSection.tsx` (76) | `TemplatePhaseSection.tsx` (54) | template = release minus collapse / `readOnly` / done-count — one generic, optional props |
| `releases/components/TaskRow.tsx` (155) | `TemplateTaskRow.tsx` (98) | template = release minus checkbox / notes / `readOnly` — one generic |

Page logic is forked too: `TemplatesPage.tsx:106-124` (`move`) is line-for-line `ReleaseDetailPage.tsx:148-164`
(swap → renumber → persist-with-revert); `TemplatesPage.tsx:51-60` (`byPhase`) ≡ `ReleaseDetailPage.tsx:77-86`;
`TemplatesPage.tsx:72-85` mirrors `ReleaseDetailPage.tsx:111-126`. Extract `byPhase` into `lib/phase.ts`
(exists) and let the reorder mutation live in the query layer.

### 6. Extract the repeated list-page shell
Highest duplication density in the codebase. New primitives in `components/`:
- **`<DataTable>`** — the wrapper (`overflow-x-auto rounded-xl border border-edge bg-panel`, **5×**), the
  `<thead>` (**5×**), and the row class (`cursor-pointer border-b border-edge/50 last:border-b-0 hover:bg-edge/40`,
  **5×**) across `AllReleasesPage:155`, `ArchivedReleasesPage:74`, `CatalogPage:133`, `ArchivedSongsPage:73`,
  `ArtistsPage:87`. **Keep `overflow-x-auto`** — it is the M22 mobile-clipping fix.
- **`<EmptyState>`** — promote `features/home/components/EmptyState.tsx` to `components/`; 8 copies of
  `rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center`
  (`ReleaseFormPage:141` and `SongFormPage:76` are near-identical "need at least one artist" blocks).
- **`<ErrorBanner>`** — 12 copies in 2 variants (single string ×8; `<ul>` of `errors[]` ×4, all four
  wrapping the identical `{errors.map(msg => <li key={msg}>{msg}</li>)}`).
- **`<Loading>`** — 13 copies of `<p className="text-slate-400">Loading…</p>`.
- **`<FilterBar>`** — `HomePage:81-112` / `AllReleasesPage:81-120` share the artist+type selects and Clear
  almost verbatim; `CatalogPage:93-122` shares the artist half. The `hasFilters` useMemo appears 3×.
- **`useConfirmDelete`** — the confirm → delete → toast → reload shape appears 6×
  (`ArchivedReleasesPage:37-53`, `ArchivedSongsPage:36-52`, `CatalogPage:47-63` and `:65-81`,
  `ArtistsPage:52-67`, `TemplatesPage:87-104`), two byte-identical but for the api call. With Query,
  `reload` becomes `invalidateQueries`.

### 7. Split `ReleaseDetailPage` (416 lines — the one true god component)
8 `useState` (`:26-35`), 3 `useEffect` (`:60-72`), 3 `useMemo`, and 11 async handlers mixing fetch +
optimistic mutation + revert + business rules + render. Extract:
- **`useReleaseTasks(id)`** — toggle/add/update/remove/move (`:89-164`)
- **`useReleaseTracks(id)`** — add/addExisting/toggleFocus/remove/move (`:189-293`)

Both built on the Query mutations from task 3. Target: page under ~150 lines, render only.

`ReleaseFormPage.tsx` (267): collapse the 9 field `useState`s (`:24-40`) into one `useReducer`; lift
validation (`:79-99`) out of submit (`:71-135`).

### 8. Types and lint
- Move the inline API input types into `types/`: `tasks.ts:6-9` ≡ `templates.ts:7-9` (byte-identical),
  `tasks.ts:14-22` ≈ `templates.ts:15-23`; also `releases.ts:11-17`, `songs.ts:12`.
- **Union the status.** `types/release.ts:26,40` type it bare `string`, so the four `ReleaseStatus.cs:11-14`
  constants go unenforced across `StatusBadge.tsx:1`, `AllReleasesPage.tsx:103-105`, `format.ts:43` →
  `type ReleaseStatus = 'Upcoming' | 'Released' | 'Complete' | 'Archived'`.
- Add `clsx` + `cva`. Three variant maps already hand-roll CVA's exact pattern (`Button.tsx:11-16`,
  `RowMenu.tsx:95-99`, `Toast.tsx:9-13`, `StatusBadge.tsx:1-6`). Also cleans up the `${x ? cls : ''}`
  double-space ternaries (`ReleaseFormPage.tsx:157,190`, `SongFormPage.tsx:92`, `ArtistFormPage.tsx:77`).
- `eslint.config.js`: move to `tseslint.configs.recommendedTypeChecked` for `no-floating-promises` — it
  catches the fire-and-forget `load()` at `ReleaseDetailPage.tsx:299`, `HomePage.tsx:63`, `CatalogPage.tsx:59`.
- **Note:** enums are ints matching `System.Text.Json` defaults (`types/enums.ts:1`), so numeric values stay
  silently coupled to C# declaration order. No codegen exists. Out of scope; keep mirroring both sides.

### 9. Comments — leave them alone (mostly)
The audit found **no over-documentation** on the web side. Highest density is `lib/calendar.ts` at 34%, and
those are load-bearing (the UTC trap `:3-5`, why `weekCount` is computed `:46-51`). These are regression
notes for real, fixed bugs and **must survive the refactor**:
- `ReleaseFormPage.tsx:243-244` — why both buttons need an explicit `type` (the HTML default is `submit`,
  which re-fired the create POST and **duplicated the release**)
- `InlineAddForm.tsx:10-11` — same bug class
- `RowMenu.tsx:10-14`, `SoftWarning.tsx:12-14` — why the portal is required (`fixed` resolves against a
  transformed ancestor)
- `Modal.tsx:24-26` — why focus is conditional
- `tailwind.config.js:12-13` — why both keyframes keep the X offset

**Only stale comment to delete:** `ReleaseFormPage.tsx:11-12` (dies with `TEMPLATE_TASK_COUNT`).

---

## M25 — API + tests refactor

### 1. Close the archived-release write gap (defect 1)
Enforce the rule the whole read side already assumes. Put it in **Domain**, per the CLAUDE.md convention:
- New `Zmg.Domain/ReleaseMutability.cs` — pure, matching the style of `SongArchival` / `ReleaseStatus`.
- Guard `ReleaseService.UpdateAsync` (`:205`), plus `TrackService.AddAsync/DeleteAsync/ReorderAsync/
  ToggleFocusAsync` and `ReleaseTaskService.ToggleAsync/UpdateAsync/AddAsync/DeleteAsync` — none of which
  load release archived state today.
- Return **409 Conflict**, matching `SongService.cs:109-110`'s precedent for the same rule on songs.
- Unit-test the rule in Domain; one integration test per service proving the wiring.

### 2. Hoist the duplicated business rule (the only real logic leak)
The "new inline song title clashes with an active same-artist song" rule exists **three times, two of them
divergent**: `ReleaseService.cs:123-138` (with within-request `HashSet` dedupe, → 400) vs
`TrackService.cs:49-55` (no dedupe, → 400) — while a correct pure version already sits **unused** in
`Validation.ValidateSong` (`Validation.cs:150-155`) using the same `DuplicateSongTitleMessage` const. This
contradicts CLAUDE.md's own convention. Make both services call the Domain function.
- Also fold in the **new-track title dedupe** that only exists in the service today:
  `Validation.ValidateReleaseTracks` de-dups *ids* only (`Validation.cs:181-186`), which is why
  `TrackApiTests.cs:151` has to prove it at the HTTP level. It's a domain rule; move it.
- Expose `DuplicateSongTitleMessage` as the stable, matchable value for M24 task 4.

### 3. Query efficiency
- **`AsNoTracking` appears zero times in the solution.** Add it to every read-only path:
  `ReleaseService.ListAsync`, `SongService.ListAsync/GetAsync`, `TemplateService.ListAsync`, both
  `PendingService` methods. (The `Select`-projection paths are already fine.)
- **`PendingService.ListAsync` (`:16-39`) loads the entire database on every Home load** — all non-archived
  releases + `Tasks` + `Tracks`, then all non-archived songs + `ReleaseLinks` → `Release` → `Tasks`,
  **fetching release tasks twice**. No pagination. Narrow it.
  *Trade-off, decide during implementation:* pushing too far toward SQL drags `PendingActions.Compute` out
  of Domain. **Prefer projecting only the fields `Compute` needs over moving the rule.**
- **`SongQueryExtensions.WithDetailIncludes`** — `SongService` repeats an identical 4-level `Include` chain
  **3×** (`:62-67`, `:93-98`, `:139-144`). Mirror the proven `ReleaseQueryExtensions.cs:12` pattern.
- Drop the redundant re-query after save (`ReleaseService.cs:201,237`, `SongService.cs:93-98,139-144` —
  re-fetching an already-tracked graph) and `ArtistService.cs:65-67`'s 3 extra `CountAsync` round-trips
  (which duplicate the projection at `:20`/`:27`).
- `SongService.ListAsync` (`:38-56`) emits ~6 correlated subqueries per row — single round-trip, scales
  badly. Note it; revisit only if the catalog grows.
- **Leave the in-memory status filter** (`ReleaseService.cs:71`): status is derived by `ReleaseStatus.Derive`
  and can't move to SQL without breaking the pure-Domain design. Note it, don't "fix" it.

### 4. Ship `canArchive` on the release DTOs
`ReleaseListItem` / `ReleaseDetail` carry no `canArchive`, so the web re-derives the rule in 3 places
(M24 task 4) — inconsistent with `SongListItem.canArchive`, which the codebase already ships. Compute it
server-side beside the existing `Status` / `Warnings` derivation (`ReleaseService.cs:67-69`).

### 5. Thread `CancellationToken`
**Zero occurrences in the entire solution.** Mechanical, wide, low-risk: ~40 service methods, 7 interfaces,
all endpoints (minimal APIs bind it automatically). Pure upside.

### 6. Split `ReleaseService.CreateAsync`
119 lines (`:85-203`) doing validation + song resolution + title-clash + template copy + backfill + track
materialization — the only genuinely fat method in the API. Decompose into private steps; the extracted
title-clash rule (task 2) already removes one chunk.

### 7. Fix the Dockerfile (defect 2)
- Add `COPY src/Zmg.Infra/ src/Zmg.Infra/` to stage 2 (`Dockerfile:21-23`). Then **actually run**
  `docker build -t zmg-tracker .` — PROGRESS.md's backlog has wanted this since M12.
- **Guard the null connection string.** `appsettings.json` has **no `ConnectionStrings` section** (only
  `appsettings.Development.json:8-10` does), so Production depends entirely on `ConnectionStrings__Zmg`
  from the Dockerfile that can't build — and `Program.cs:9-10` passes the possibly-null string to
  `UseSqlite` unguarded. Fail fast with a clear message.
- Note `Program.cs:32-36` runs `db.Database.Migrate()` at startup unconditionally, including Production.
  Acceptable for a single-instance SQLite app; documenting, not changing.

### 8. Tests — defuse the date bomb first (defect 4)
One shared relative-date helper. Replace the ~14 `new DateOnly(2026, 8, 14)` sites
(`ArtistReleaseApiTests:97`, `ReleaseIdentifierApiTests:38,43,57,73`, `TemplateApiTests:150`,
`TaskTimeframeApiTests:23`, `ReleaseTaskApiTests:18`, `TrackApiTests:27,117,121,146,158,330`). Four files
already do this right (`PendingApiTests:15`, `ReleaseArchiveApiTests:15`, `ReleaseListScopeApiTests:11`,
`SongArchiveApiTests:14`) — follow them. Also unify the **4 different hardcoded "today"s** in Domain.Tests
(`ReleaseStatusTests:5` and `ValidationTests:7` → `2026-07-11`, `PendingActionsTests:9` → `2026-07-12`,
`SongArchivalTests:9` → `2026-07-15`); there is no reason for them to differ.

### 9. Tests — shared fixtures and one lifecycle
- **Kill the setup duplication.** 8 `CreateArtist` copies **with 3 different signatures** (some return
  `ArtistDto`, some `Guid`; some `static`, some not) — the asymmetry already produces confusing call sites
  (`SongArchiveApiTests:231` passes a bare `Guid` where `SongApiTests:196` passes `artist.Id`). Plus 9
  release-creation copies, 4 `NewTrack` copies (two identically-named, differently-signatured), and ~20
  repeats of the `CreatedWithWarnings<T>.Data` unwrap. → one `ApiTestBase` + an `ApiClient` wrapper hiding
  the unwrap.
- **Domain.Tests has no helper file at all** — 6 private builders across 6 files (`ProgressTests.Task:8` ≈
  `PendingActionsTests.Task:26`; `SongArchivalTests.Rel:11` ≈ `PendingActionsTests.Rel:11`). → one
  ObjectMother (`ReleaseBuilder`/`SongBuilder`/`TaskBuilder`). Model to copy: `SongArchivalTests.SongOn:15`.
- **Three competing factory lifecycles → one.** `IClassFixture` (5 files) vs `new ZmgApiFactory()` per test
  (32 tests across 4 files) vs hand-rolled `IDisposable` (2 files — this is `IClassFixture` reinvented; its
  stated isolation rationale is exactly what `IClassFixture` already provides). **≈39 host boots → ~11.**
  Heaviest targets: `SongArchiveApiTests` (13 boots), `PendingApiTests` (8).
- Make the **implicit contract explicit**: the `IClassFixture` files depend on each test creating a
  uniquely-named artist to avoid collisions — a copy-pasted name silently breaks a neighbour. Put a
  unique-name helper in the base.
- Use the file's own helpers consistently — `ReleaseArchiveApiTests:134-145` and `TemplateApiTests:147-152`
  build fixtures with raw inline HTTP while their own helpers sit unused.
- Fix `ZmgApiFactory.cs:14`'s stale comment (claims the schema is "created via `Migrate`"; the factory never
  calls it — `Program.cs` does) and the `_connection` overwrite-on-rebuild leak at `:31`.

### 10. Tests — AAA, Theories, and deletions
- **Adopt AAA.** There are **zero** `// Arrange / Act / Assert` comments in the suite. The dominant
  anti-pattern is **act-inside-assert** (`Assert.False(Validation.ValidateArtist("  ", []).IsValid)`),
  pervasive in `ValidationTests`, `ReleaseWarningsTests`, `SongArchivalTests`, `ReleaseTests`,
  `ReleaseStatusTests`. Extract the Act everywhere. **In-repo exemplars: `ProgressTests.cs` and
  `TemplateCopyTests.cs` — both already clean; they need no work.**
- **Convert ~28 multi-act Facts to `[Theory]`.** The suite has exactly **one** Theory today
  (`PendingActionsTests.cs:101-112`) — copy its shape. Biggest wins: `ValidationTests` 15 → ~6 Theories +
  4 Facts · `ReleaseStatusTests` 5 Facts / 8 asserts → **1 Theory** · `ReleaseWarningsTests` 6 → 1+1 ·
  `ReleaseTests` 4 → 1+1 · `SeedDataTests` 4 → 1+1.
  **Do not Theory-ize `SongArchivalTests`** — its 6 scenarios differ structurally in arrange; they are
  genuinely distinct and the coverage is good.
- **Delete the integration tests a domain unit test already proves** (~32 host boots of the heaviest work,
  zero unique coverage lost): `SongArchiveApiTests:69` and `:91` (≡ `SongArchivalTests:43`/`:33`),
  `PendingApiTests:156` (**triple**-covered with `PendingActionsTests:123` *and* `ReleaseArchiveApiTests:152`),
  `PendingApiTests:83`, `PendingApiTests:97`, `TrackApiTests:74`, `TrackApiTests:126`.
- **Reduce to the unique claim:** `TrackApiTests:50` (keep only the list-DTO-agrees half, `:57-59`),
  `PendingApiTests:50` (pays 3 release creations = 93 seeded tasks to prove ordering the domain already
  proves), `PendingApiTests:131`, `ReleaseIdentifierApiTests:51` (keep `:68-69` — *the list DTO carries the
  same warning as the detail DTO* — which **is** unique).
- **Preserve the genuinely-integration tests. Do not touch:** `TemplateApiTests:141` (template-edit snapshot
  invariance — the core product guarantee; `TemplateCopy` proves the copy, not the non-propagation),
  `PendingApiTests:111` (cross-release ISRC de-dup, exists only at the service layer), `PendingApiTests:173`
  (per-release scoping), `SongApiTests:213` (composite-PK regression), `ArtistReleaseApiTests:140`
  (FK 409-not-500 regression), `ReleaseIdentifierApiTests:81` (backfill auto-check), `SongApiTests:36`
  (derived date), and all of `ReleaseListScopeApiTests` (best-designed file in the suite).
- **De-duplicate the seed counts.** 31/41/6/18/7 are asserted in **4 places across 2 projects**
  (`SeedDataTests:13-16`, `TemplateApiTests:35-37`, `ArtistReleaseApiTests:103-107`+`:122`, and
  `ReleaseTaskApiTests:135`'s hardcoded `30`). Keep one home (the domain test); assert **relatively**
  elsewhere (`before - 1`), which `TemplateApiTests:129` and `ReleaseTaskApiTests:61` already do correctly.
- **Assert against the public consts, not string fragments.** `ValidationTests:108` matches `"already exists"`
  when `Validation.DuplicateSongTitleMessage` exists at `Validation.cs:46` *precisely so tests and the SPA
  can match on it*; same for `:70` (`"past"`) and `:81` (`"already has a release"`). `ReleaseWarningsTests`
  already does this right. Note `PendingActionsTests:82` uses `SeedData.DistributeToDspsTitle` while the
  same file hardcodes the literal at `:171`.
- Also: replace the hand-rolled sortedness loop (`PendingApiTests:73-74`); remove the discard hacks that
  signal an over-built arrange (`SongApiTests:278` `_ = first;`, `SongArchiveApiTests:88`, `TrackApiTests:259`);
  fix the namespace-qualification inconsistency (`SongArchiveApiTests:263,266,272` fully qualify
  `Zmg.Domain.*` while every sibling adds `using Zmg.Domain;`); distinguish `EnsureSuccessStatusCode()`
  used as an assertion from its use as arrange-guard noise (~40 sites — it throws
  `HttpRequestException`, not an xunit failure, so messages read poorly).
- **Assertion style needs no migration.** The suite is already 100% consistent plain xunit `Assert.*` —
  its strongest quality. **Do not introduce FluentAssertions.**

### 11. Tests — close the coverage gaps
- **Unit-test `Reorder.cs`** (24 lines, no unit tests) — **three identically-named
  `Reorder_with_missing_ids_is_rejected` integration tests** guard it (`ReleaseTaskApiTests:112`,
  `TemplateApiTests:105`, `TrackApiTests:283`). Test it directly; keep one integration test.
- **Unit-test `OperationResultExtensions.cs`** (41 lines) — the `OperationResult` → `IResult` mapping is
  what ~25 status-code assertions actually cover. Testing it directly unlocks deleting most of the
  duplicated `..._is_rejected` integration tests.
- **404-on-unknown-id is tested for 4 of ~20 applicable routes** — a perfect `[Theory]` over
  `(method, urlTemplate)`. Missing on: `PUT /artists/{id}`, `PUT /releases/{id}`, `POST /releases/{id}/archive`,
  `DELETE /releases/{id}`, `GET /releases/{id}/archive-preview`, `POST /releases/{id}/tasks`, `PUT /tasks/{id}`,
  `DELETE /tasks/{id}`, `DELETE /releases/{id}/tracks/{songId}`, `PUT /template-tasks/{id}`, both `order` routes.
- **`GET /api/releases?status=` has zero tests** (`ReleaseEndpoints.cs:15`) — the only completely untested
  parameter in the suite.
- **`DELETE /api/template-tasks/{id}` "last task" 409** — `Validation.ValidateTemplateTaskDelete`
  (`Validation.cs:192`) is domain-tested (`ValidationTests:151`) but nothing proves the endpoint calls it.
- Also missing: `PUT /api/artists/{id}` duplicate-name-on-rename; `PUT /api/releases/{id}` tracklist update.
- **Thin spots:** `ProgressCalculator` (`Fraction`, `Percent` .5-boundary rounding, the
  `ArgumentNullException.ThrowIfNull` guards at `Progress.cs:22,34`, empty `Calculate`); `Validation`
  (the fluent `Error`/`Warn` chaining return at `:22-32`, the `Guid.Empty` ExistingSongId guard at `:175,182`,
  null entries in the title lists at `:60,111,152`, and warning-only ⇒ `IsValid && Errors.Empty`).
- `ValidationTests:46` asserts `Errors.Count == 3` — brittle; assert the **presence of specific errors** instead.
- **Consolidate `ReleaseTests.cs`** — `Release.NeedsWarning` and `ReleaseWarnings.Compute` MissingUpc encode
  the same rule in two files.
- **Delete `SeedDataTests:8`** (`Single_template_has_the_v1_1_counts`) — a change-detector asserting the data
  file says what the data file says. **Keep `:40`** (`Seeded_task_ids_are_unique_across_both_templates`) — the
  one real invariant, guarding copy-paste Guid mistakes in `SeedData.cs`.

### 12. Comments
Backend documentation is **high-quality and mostly *why*** — it references build plans and milestone tags and
explains non-obvious decisions (`ArtistService.cs:76-78` documents a real past bug; `ZmgDbContext.cs:80-82`
explains why the Track query filter exists). **Keep it.** Remove only the genuinely redundant:
- `PendingEndpoints.cs:16` — `// Gets pending actions for a release` above `MapGet("/{id:guid}", … ListByReleaseIdAsync)`
- The service↔endpoint paragraph duplication — `TemplateService.cs:11-14` ≡ `TemplateEndpoints.cs:7-10`;
  `ReleaseTaskService.cs:11-14` ≡ `TaskEndpoints.cs:7-10`; `TrackService.cs:11-16` ≡ `TrackEndpoints.cs:7-11`.
  Keep one copy, on the service.
- The identical `// Append position…` over the near-identical `NextSortOrder` in `ReleaseTaskService.cs:105`
  and `TemplateService.cs:109` (same shape, different DbSet — also a dedupe candidate).
- Version-archaeology noise in tests: `ReleaseTests.cs:34`, `SeedDataTests.cs:12`, `ReleaseIdentifierApiTests.cs:8`.

---

## Not in scope (deliberate)

- **Scrutor / DI auto-registration** — at 7 services, `Program.cs:12-18` + `:46-52` is ~14 explicit, greppable,
  debuggable lines. Reflection is a lateral move.
- **FluentAssertions** — a greenfield decision, not cleanup (see M25 task 10).
- **Frontend component tests** — Vitest covers the pure modules only this round.
- **OpenAPI/NSwag type codegen** — the hand-mirrored `types/` are clean; the enum-ordinal coupling is noted, not solved.
- **Seed-data 3-way materialization** — `SeedData.cs` → `InitialCreate.cs` → `ZmgDbContextModelSnapshot.cs` is a
  real drift hazard, worsened by `SeedData.DeterministicTaskId` (`:120-125`) deriving ids from
  `(templateId, phase, order)`, so **inserting a task mid-list renumbers every subsequent GUID**. But CLAUDE.md
  documents `InitialCreate` as a hard reset with no migration path. Leave it; carry the note into PROGRESS.
- **`Swashbuckle.AspNetCore` 10.2.3 vs EF 8.0.11** — worth a compatibility check someday, not now.

---

## Files at a glance

**New (web):** `api/queries.ts`, `components/DataTable.tsx`, `components/EmptyState.tsx`,
`components/ErrorBanner.tsx`, `components/Loading.tsx`, `components/FilterBar.tsx`, `hooks/useConfirmDelete.ts`,
`hooks/useDebouncedValue.ts`, `features/releases/hooks/useReleaseTasks.ts`,
`features/releases/hooks/useReleaseTracks.ts`, `lib/*.test.ts`, `hooks/*.test.ts`.
**New (backend):** `Zmg.Domain/ReleaseMutability.cs`, `Zmg.Api/Extensions/SongQueryExtensions.cs`,
`tests/Zmg.Api.Tests/ApiTestBase.cs`, `tests/Zmg.Domain.Tests/Builders.cs`,
`tests/Zmg.Api.Tests/OperationResultExtensionsTests.cs`, `tests/Zmg.Domain.Tests/ReorderTests.cs`.
**Removed (web):** `templates/components/TemplateMovePhaseItems.tsx`, `TemplateTimeframeEditor.tsx`,
`TemplatePhaseSection.tsx`, `TemplateTaskRow.tsx`; `features/home/components/EmptyState.tsx` (promoted to `components/`).
**Modified (web):** `tsconfig.app.json`, `package.json`, `eslint.config.js`, `App.tsx`, all 5 list pages,
`ReleaseDetailPage.tsx`, `ReleaseFormPage.tsx`, `SongDetailPage.tsx`, `SongFormPage.tsx`, `TemplatesPage.tsx`,
`SongPickerModal.tsx`, `types/*`, `api/*`, `lib/format.ts`, `lib/phase.ts`, `components/*`.
**Modified (backend):** `Dockerfile`, `appsettings.json`, `Program.cs`, all 7 services + interfaces,
`Contracts/Dtos.cs`, `Validation.cs`, `Reorder.cs`, and every test file in both test projects.

---

## Verification

CLAUDE.md's blast-radius rule says scope verification to what the change can break — but 2.3 changes DTOs
(`canArchive`), endpoints, and both sides, so **this is the case that needs everything**.

1. **M24 gate 1:** `npm run build` clean with `strict: true`, **before any code moves**. Commit alone.
2. **M24 gate 2:** `npm run test` green (calendar / format / usePersistedState).
3. **M24 final:** `npm run lint` + `npm run build` clean. Then **drive the app** at desktop and 375px:
   - Artists load **once** across navigation (Network tab: one `/api/artists`, not 8).
   - Release detail: toggle/add/rename/reorder/delete tasks; add/reorder/focus/remove tracks. Optimistic
     updates still instant; revert-on-error still works.
   - Templates page: the same task flows via the now-shared generics; **editing a template still does not
     touch existing releases**.
   - Create-release hint shows the **live** template count — edit a template, reload, confirm it changed.
   - All 5 list pages: tables, filters, empty/error/loading states, kebabs, no mobile clipping (M22 fix).
   - Calendar: month grid, Today, next-release chip, preview modal, archive-from-preview.
   - Archive affordance appears/disappears per the **server's** `canArchive`.
   - Creating a release with a duplicate song title still prompts correctly (no error-string parsing).
4. **M25:** full `dotnet test` green. Specifically:
   - **New:** `PUT` / task-write / track-write against an **archived** release each return **409**.
   - `Reorder` + `OperationResult` mapping covered by unit tests; the redundant integration tests are gone.
   - Host boots down from ~39 to ~11 — confirm suite wall-clock drops.
   - **Prove the date bomb is defused** — no `2026-08-14` literals remain (`grep`); the date-helper tests pass.
5. **Docker (defect 2):** `docker build -t zmg-tracker .` **must actually succeed**, then
   `docker run -p 8080:8080 -v zmg-data:/data zmg-tracker` → app on :8080, SQLite persists across restarts.
   **This has never once been run.**
6. **Finish:** update `plans/PROGRESS.md` — a journal entry for v2.3, and reset "Backlog / next steps" to
   Phase 2 (DSP stats), carrying forward the deferred seed-drift note.
