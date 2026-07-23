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
- [build-plan-2.3.md](build-plan-2.3.md) — refactor · code health (M24–M25). Shipped.
- [build-plan-2.4.md](build-plan-2.4.md) — UI polish · dark/light (M26–M28). Shipped.
- [build-plan-2.5.md](build-plan-2.5.md) — deployment · ACA/Neon/R2/Terraform/CI-CD (M29–M34). Shipped.

Newer plan versions go in new `build-plan-N.N.md` files; older ones stay frozen.

**Current state:** feature-complete through **v2.4** and **fully deployed** — **v2.5 (M29–M34) is
complete**. Live on **Azure Container Apps** over **Neon Postgres**, covers in **Cloudflare R2**
(normalized to a 1000px WebP on ingest), the whole stack codified in Terraform under
[`infra/`](../infra/README.md), and a **GitHub Actions pipeline** that builds + pushes on every green
push to main and deploys via OIDC (M34). Backend **domain 119 / API 156**, SPA **32 Vitest** — the
pipeline gates on these. **No milestone open** — next is **Phase 2** (DSP stats first; also SPA/Pages
split, cold-start tuning, real-Postgres tests), which starts a new `build-plan-3.0.md`.

> ⚠️ **DB is Postgres (Neon) as of v2.5/M30.** Dev + prod both use `ConnectionStrings__Zmg` — **dev** via
> `dotnet user-secrets` in `src/Zmg.Api` (never commit it), **prod** as an ACA secret. Startup applies
> migrations + seeds. Reset local data by resetting the Neon branch or
> `dotnet ef database drop` + `database update`. Tests run **SQLite in-memory**. Keep EF tooling on **EF 8** to match the runtime.

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

**v2.3 (M24–M25) — refactor · code health, no features.** Web: `strict` across the SPA, **Vitest**
added, **TanStack Query** adopted over the `api/` modules, a shared list-page shell extracted, and the
`ReleaseDetail`/`ReleaseForm` god-components split. API: archived releases now reject **writes** with a
409, the Dockerfile fail-fasts on a null connection string, a relative-date `TestDates` defused the test
date bomb, and `canArchive` moved server-side onto the release DTOs. A test-hygiene sweep followed.

**v2.4 (M26–M28) — UI polish · dark/light (SPA-only).** lucide icons, a headerless right-aligned action
column, badge-folding on the mobile release tables, and self-sizing Home cards (M26); then the
hardcoded neutrals were routed through CSS-variable tokens as a deliberate visual no-op (M27), which
the **dark/light toggle** cashed in immediately after (M28) — OS-following until explicitly toggled,
persisted, and applied pre-paint. **+5 Vitest → 32** web tests.

**v2.5 (M29–M34) — deployment.** First hosting: the container image on **Azure Container Apps**
(Consumption, scale-to-zero) (M29); prod off ephemeral SQLite onto **Neon Postgres** via EF Npgsql
(M30); release covers into **Cloudflare R2** through an upload/paste-URL tile that re-stores remote URLs
server-side rather than hotlinking (M31), normalized to a 1000px WebP on ingest (M33); the whole stack
codified in **Terraform** across `azurerm` + `neon` + `cloudflare`, **imported** rather than recreated
so prod never moved (M32); and a **GitHub Actions pipeline** that builds a SHA-tagged image on every
green push to main and deploys to ACA via **OIDC**, with a `workflow_dispatch` rollback to any prior tag
(M34).

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
- **Colours go through semantic tokens, never raw Tailwind neutrals (v2.4).** `src/index.css` carries the
  dark RGB channels on `:root` and overrides only what changes under `:root[data-theme="light"]`;
  `tailwind.config.js` wraps each token in `rgb(var(--token) / <alpha-value>)` so opacity modifiers keep
  working. Two traps: `text-strong` flips to dark slate in light, so any **saturated/accent solid** fill
  needs an explicit `text-white`; and the theme must be stamped onto `<html>` by the inline `index.html`
  script **pre-paint**, or a light reload flashes dark.
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
- **User-supplied images are accepted on their bytes, and remote fetches are guarded (M31).** Cover
  ingest trusts the **magic number**, never the declared content-type, and caps size by a capped read
  rather than `Content-Length`. Any future server-side fetch of a user-supplied URL must reuse
  `CoverImage`'s SSRF guards — scheme allow-list, resolve-then-check every address, and follow redirects
  **manually** so each hop is re-checked (auto-redirect hands the attacker the second request for free).
- **Cover encoding: `WebpEncoder.FileFormat` must be set to `Lossy` explicitly, and ImageSharp stays on
  3.1.x (M33).** At its default the encoder emits **lossless** WebP (`VP8L`) where `Quality` is ignored —
  a 4.3 MB source came back at 2.9 MB instead of 584 KB, with the unit tests perfectly green. Do not let
  the package float to **4.0.0**, which added a build-time licence check (a `sixlabors.lic` file must be
  present to compile) that would break the Dockerfile and CI.
- **Terraform (`infra/`) owns infrastructure; the pipeline owns the image tag.** The container app
  `ignore_changes = [...image]`, so deploys ship a new tag without Terraform reverting it and CI needs no
  state access; `var.container_image` is a bootstrap default, not the live tag. The config was
  **imported**, so it must match reality — any `forces replacement` is a config bug, and on
  `neon_project` / `cloudflare_r2_bucket` it means destroying the production database / every cover.
- **Deploy is a GitHub Actions pipeline over an immutable SHA-tagged image.** `ci.yml` tests → builds +
  pushes `ghcr.io/…:{short-sha}` → calls reusable `deploy.yml` on green pushes to main; `deploy.yml`'s
  `workflow_dispatch` re-points ACA at any existing tag (rollback, never rebuilds) — **build once,
  deploy separately**. Azure auth is **OIDC**, no stored secret: the token subject
  `repo:…:environment:production` must equal the GitHub Environment name exactly (else `AADSTS70021`).
  Traps: `cache-to: type=gha` needs a `setup-buildx-action` step; `GITHUB_TOKEN` pushes only to a GHCR
  package the repo is linked to with Write; `id-token: write` must be on the **calling** job; pin the
  `docker/*` + `azure/login` majors from the live registry (Node-24 releases), not memory.
- **Prod runs Postgres (Neon); integration tests run SQLite in-memory (v2.5/M30).** Migrations are
  Postgres-specific. Keep query code **provider-agnostic** — e.g.
  title search lowercases both sides of `Like` rather than using Npgsql `ILike` — so SQLite tests stay
  representative. Real-Postgres tests (Testcontainers + CI service container) are deferred to Phase 2.

---

## Project layout

```
src/Zmg.Domain   entities/enums, template-copy, progress, status, validation, seed,
                 release-warnings, song-archival, pending-actions  (pure, no I/O)
src/Zmg.Api      minimal API: endpoints, service layer (+ interfaces), DTO contracts, extensions
src/Zmg.Infra    EF Core + Npgsql/Postgres: ZmgDbContext (seeding) + migrations
src/Zmg.Web      React + Vite + Tailwind SPA, organized by feature folder
tests/Zmg.Domain.Tests   xUnit unit tests
tests/Zmg.Api.Tests      integration tests (WebApplicationFactory + in-memory SQLite)
infra                    Terraform: azurerm + neon + cloudflare in one root module (see infra/README.md)
```

---

## Backlog / next steps

- **Shipped — v2.4 (M26–M28):** UI polish · semantic color tokens · dark/light toggle.
- **Shipped — v2.5 (M29–M34):** ACA deploy · Neon Postgres · R2 covers · cover normalization · Terraform ·
  CI/CD image pipeline. **v2.5 complete — no milestone open.**
- **Next: Phase 2 — DSP stats** (the reason this exists over Notion/Trello): hang streaming/revenue data
  off the stable Artist / Release / **Song** / Track ids and the UPC/ISRC columns; the v2.0 Song ids are
  its foundation. No build plan yet — write `build-plan-3.0.md` when it starts.
- **Infra hardening (not gating):** Terraform state is **local on one machine** — move to a remote
  encrypted backend (Azure Storage + locking) before anyone else applies; and add a `terraform fmt
  -check` + `validate` CI job so `infra/` drift is caught in review. Neither blocks; both are cheap.
- **Still open (not gating):** Low-value test polish (exhaustive AAA pass, the last few Theory
  conversions). The suite is green without it.
- **Per-track task fan-out** on albums: registrations that repeat per track are single "per track" tasks
  today. Decide after the first real album.
- Deferred: un-archive/restore and hard-delete/purge (archives are terminal by rule); auth for hosted
  deploys; absolute per-task due dates (v1.1 only added timeframe *ranges*). Also carried forward from
  the M24 audit: the **seed-data 3-way drift hazard** (`SeedData.cs` → `InitialCreate` → snapshot, with
  `DeterministicTaskId` renumbering every later GUID on a mid-list insert) — left as-is per CLAUDE.md's
  hard-reset rule, noted here so Phase 2 doesn't rediscover it.
