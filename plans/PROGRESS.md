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
- [build-plan-2.3.md](build-plan-2.3.md) — refactor · code health (M24–M25). Shipped. A live
  `docker build` verify is the one non-gating item still open (see backlog).
- [build-plan-2.4.md](build-plan-2.4.md) — UI polish · dark/light groundwork (M26–M27). Shipped.

Newer plan versions go in new `build-plan-N.N.md` files; older ones stay frozen.

**Current state:** feature-complete through **v2.4** — a SPA-only UI-polish + color-tokens pass. Backend
tests **domain 73 / API 136**, green (~6s); SPA **28 Vitest**. One non-gating item remains: the pending
live `docker build` verify. Next is the **dark/light toggle** (values-only override on the v2.4 token
groundwork), then **Phase 2 — DSP stats** (no build plan yet).

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

**v2.3 (M24–M25) — refactor · code health, no features.** Web (M24): `strict` on across the SPA,
**Vitest** added (28 tests on the pure modules), **TanStack Query** adopted over the `api/` modules so
the artist roster caches across navigation, a shared list-page shell extracted, the `Template*` fork
collapsed into generic task components, and the `ReleaseDetail`/`ReleaseForm` god-components split;
`todayIso()` fixed to local date, the stale template constant replaced by a live count, cva for the
variant maps + typed ESLint. API (M25): closed four defects — archived releases now reject **writes**
with a 409 (pure `ReleaseMutability`), the Dockerfile copies `Zmg.Infra` + fail-fasts on a null
connection string, and a relative-date `TestDates` defused the test date bomb — plus the title-clash
rule hoisted into Domain, `canArchive` derived server-side onto the release DTOs (`ReleaseArchival`),
`AsNoTracking` read paths, and `CancellationToken` threaded throughout. A **test-hygiene sweep**
followed (Domain ObjectMother, `IClassFixture` to cut host boots, redundant integration tests pruned),
and the two parked web items closed — the SPA reads `canArchive` from the DTO and mirrors the
duplicate-title constant. The Dockerfile fix's live `docker build` is still unverified (daemon was down).

**v2.4 M26 — UI polish (SPA-only).** Four user-visible fixes: **lucide-react** added and the `RowMenu`
kebab glyph swapped from the literal `⋮` to `<EllipsisVertical />` (updates all five call sites at once);
`DataTable`'s `headers` evolved from `string[]` to `{ label; className? }[]` so the action column can be
**headerless + right-aligned** (retiring the "Action"/"Actions" label inconsistency) and columns can be
responsive-hidden; the mobile **Releases** table now folds Type/Status badges under the name below `sm`
(`hidden sm:table-cell` on those columns) so the kebab is reachable without sideways scroll — same
Type-fold on Archived Releases; and the **Home** card grid got `items-start` so cards size to their own
content instead of stretching to the tallest in the row (verified `align-items: flex-start`, mixed
458/318px heights). No API/DTO/domain change → lint + build + browser drive only, no `dotnet test`.

**v2.4 M27 — semantic color tokens (SPA-only, visual no-op).** Routed the hardcoded dark neutrals
through theme-swappable CSS-variable tokens so a later dark/light plan is a values-only override, not a
43-file rewrite. Tokens live in `src/index.css` `:root` as space-separated RGB channels
(`--ink`/`--panel`/`--edge`/`--accent` + four text tokens `--strong`/`--body`/`--muted`/`--subtle`);
`tailwind.config.js` backs each color with `rgb(var(--token) / <alpha-value>)` so the existing opacity
modifiers (`bg-ink/80`, `border-edge/50`) keep working (verified `border-edge/50` → `rgba(37,42,52,0.5)`).
The 165 neutral text utilities across 42 files were mechanically remapped (`text-white`/`slate-100`→
`text-strong`, `slate-200/300`→`text-body`, `slate-400`/`gray-400`→`text-muted`, `slate-500/600`→
`text-subtle`), prefixes carried along. Stragglers left raw on purpose: the Archived **status badge**'s
slate tint (badges stay raw until the dark/light plan designs both themes together), Toast `slate-700`
and Modal `black/50` (no exact token → changing them would break the no-op). Verified a true no-op:
computed styles resolve to the exact original values (`--ink` rgb(15,17,21), `--strong` rgb(255,255,255),
`--muted` rgb(148,163,184)), colors identical across pages. Payoff for the toggle plan: add
`darkMode: 'class'`/`[data-theme]`, a `:root[data-theme='light']` channel override, and a
`usePersistedState` toggle — no JSX changes.

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

- **Next — dark/light toggle:** the v2.4 M27 groundwork is in place (semantic CSS-variable tokens). Flip
  it on with `darkMode: 'class'`/`[data-theme]` in the config, a `:root[data-theme='light']` channel
  override, and a `usePersistedState` toggle — no JSX changes. Tokenize the status badges (amber/red/
  emerald/green + the Archived slate tint) as part of this, designing both themes' badge treatments
  together. No build plan yet.
- **Deferred — Phase 2: DSP stats** (the reason this exists over Notion/Trello): hang streaming/revenue data
  off the stable Artist / Release / **Song** / Track ids and the UPC/ISRC columns; the v2.0 Song ids are
  its foundation. No build plan yet — write `build-plan-3.0.md` when it starts.
- **Still open (not gating):** **verify `docker build` on a live daemon** (the Dockerfile fix is unverified
  — daemon was down here); plus low-value test polish (exhaustive AAA pass, the last few Theory
  conversions). The suite is green without either.
- **Per-track task fan-out** on albums: registrations that repeat per track are single "per track" tasks
  today. Decide after the first real album.
- Deferred: un-archive/restore and hard-delete/purge (archives are terminal by rule); auth for hosted
  deploys; absolute per-task due dates (v1.1 only added timeframe *ranges*). Also carried forward from
  the M24 audit: the **seed-data 3-way drift hazard** (`SeedData.cs` → `InitialCreate` → snapshot, with
  `DeterministicTaskId` renumbering every later GUID on a mid-list insert) — left as-is per CLAUDE.md's
  hard-reset rule, noted here so Phase 2 doesn't rediscover it.
