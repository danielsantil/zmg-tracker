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
- [build-plan-2.6.md](build-plan-2.6.md) — hardening · hard-delete · navbar · catalog fixes (M35–M38). Shipped.
- [build-plan-2.7.md](build-plan-2.7.md) — infra hardening · remote state · cold start (M39–M42). Shipped.
- [build-plan-2.8.md](build-plan-2.8.md) — multilingual EN/ES (M43–M48). Shipped.

Newer plan versions go in new `build-plan-N.N.md` files; older ones stay frozen.

**Current state:** feature-complete through **v2.4**, **fully deployed**, and **bilingual EN/ES** as of
**v2.8 (M43–M48)** — which is complete on `feat/i18n-multilingual` but **not yet merged or deployed**,
and carries two additive-only migrations the pipeline applies before the image swaps. The SPA serves
from a **Cloudflare Worker** at the edge with `/api/*` proxied same-origin to **Azure Container Apps**
over **Neon Postgres**; covers live in **Cloudflare R2**; the hosted stack is codified in Terraform
under [`infra/`](../infra/README.md), with remote state in Azure Storage. A **GitHub Actions pipeline**
tests, builds a SHA-tagged image, applies migrations, deploys to ACA over OIDC, then ships the SPA to
Cloudflare. Backend **domain 138 / API 216**, SPA **50 Vitest** — the pipeline gates on these.
**Phase 2** (DSP stats, real-Postgres tests) is next and starts a new `build-plan-3.0.md`.

> ⚠️ **DB is Postgres (Neon) as of v2.5/M30.** Dev + prod both use `ConnectionStrings__Zmg` — **dev** via
> `dotnet user-secrets` in `src/Zmg.Api` (never commit it), **prod** as an ACA secret. **Dev and tests
> migrate at startup; prod does not** — the deploy pipeline applies migrations. Reset local data by
> resetting the Neon branch or `dotnet ef database drop` + `database update`. Tests run **SQLite
> in-memory**. Keep EF tooling on **EF 8** to match the runtime.

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
Pre); the album template is that list plus 10 extras, so **41**.

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

**v2.6 (M35–M38) — hardening.** Startup env-var **fail-fast** naming every missing key at once, with R2
now required at boot (M35); **hard-delete replacing soft-delete** app-wide, dropping `DeletedAt` and the
three query filters (M36); the nav extracted into a responsive `NavBar` that collapses below `sm` (M37);
and the catalog's release-counting reduced to one source of truth, excluding archived links everywhere
and dropping `isOrphan`/`canArchive` in favour of client-derived state (M38).

**v2.7 (M39–M42) — infra hardening · cold start.** Terraform state moved off one laptop into an
encrypted, versioned Azure Storage container with blob-lease locking (M39); `[boot]` timing logs
established a cold-start baseline (M40); the API boot path went **2.0s → 0.15s** by moving migrations
into the deploy pipeline and switching to a chiseled base image (M41); and the SPA moved to a
**Cloudflare Worker** serving it from the edge with `/api/*` proxied to ACA on the same origin (M42).
The measurement was the point: cold start is dominated by ~11–12s of Azure sandbox provisioning that no
code change touches, so M41's 1.85s was invisible end-to-end and only M42 — shell in **0.145s** instead
of a blank page for the full ~17–22s — changed what a user actually experiences. ReadyToRun and an
early `/api/health` wake were both measured and cut. See [build-plan-2.7.md](build-plan-2.7.md).

**v2.8 (M43–M48) — multilingual EN/ES.** Three bodies of text, three mechanisms: UI chrome moved onto
bundled react-i18next JSON (M43–M45), every server-minted sentence became a culture-invariant code the
SPA renders (M46), and checklist task text became *data* — a stable code per seeded task resolving a
per-locale row at read time, with Spanish seeded and correctable in the templates editor (M47–M48). M47
also fixed the latent bug the rest would have triggered: `Release.IsDistributed` matched an English task
title, so one Spanish title would have silently stopped the UPC warning, the pending engine and the
past-date backfill together. Tests **domain 119 → 138, API 158 → 216, web 32 → 50**.
See [build-plan-2.8.md](build-plan-2.8.md).

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
- **Hard-delete (M36, was soft-delete v1.2–v2.5).** DELETE removes the row. `Release.Remove` **cascades**
  to its `ReleaseTask`s and `Track` links; `Song` delete `RemoveRange`s its `Track` links first (the
  `Track`→`Song` FK is Restrict) then removes the song, its feats/collabs cascading. Delete is reachable
  **only** for an already-archived or orphan entity (a released item must be archived first, and archive
  is kept), so the soft-delete rationale — preserving stable ids for phase-2 stats — was moot: those
  entities carry no stats worth keeping. `DeletedAt`, the three `HasQueryFilter`s (Release, Song, and the
  Track filter that existed only because its parents were filtered), and the `DropSoftDelete` migration
  are all gone.
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
- **Artist delete blocks on *active* references only; archived ones cascade away with the artist.**
  `ArtistDto`'s `releaseCount`/`songCount`/`creditCount` are **active (non-archived) counts** — they're
  what the artists table shows *and* the up-front delete block — so archived work no longer inflates them.
  `ArtistService.DeleteAsync` 409s only when an active release/song is main-artist'd on the artist or it's
  credited on a *non-archived* song. With no active refs the delete **succeeds and hard-removes the
  archived data that references it**: archived releases (cascade → tasks + tracks), archived songs
  (`RemoveRange` their `Track` links first — `Track`→`Song` is Restrict — then the song, its credits
  cascading), and the artist's own feat/collab credit rows on *other* artists' archived songs (the credit
  row only, never the other artist's song). `ArtistDto` also carries `archivedReleaseCount` /
  `archivedSongCount` so the confirm dialog can warn ("N archived releases/songs will also be permanently
  removed") before the cascade. Contrast the entity-level rule above: `Release`/`Song` delete is reachable
  only for already-archived/orphan rows, whereas *this* cascade is the artist-delete cleanup path.
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
- **EF tooling must match the runtime (EF 8).** Pinned in `.config/dotnet-tools.json`; a 10.x-generated
  migration builds fine and then **silently fails at runtime** (`no such table: __EFMigrationsHistory`).
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
- **State is remote, locked, and holds live secrets (M39).** It lives in Azure Storage in a resource group
  **separate from `zmg-rg`**, so a `terraform destroy` can't delete the file describing what it destroys.
  Shared-key access is **disabled** — there is no account key, so access requires an Entra identity with
  **Storage Blob Data Contributor** (hence `use_azuread_auth`), and a fresh role assignment takes 2–5 min
  to propagate (a 403 straight after granting is propagation, not misconfiguration). Locking is a blob
  lease and fires on `plan` too; an interrupted run leaves it held → `terraform force-unlock <ID>`.
  **Two things are hand-created, deliberately:** the state backend (Terraform can't create the account its
  own state lives in) and the Cloudflare Worker (managing it would need Workers Scripts · Edit added to
  the deliberately R2-only Cloudflare token).
- **Deploy is a GitHub Actions pipeline over an immutable SHA-tagged image.** `ci.yml` tests → builds +
  pushes `ghcr.io/…:{short-sha}` → calls reusable `deploy.yml`, then `web.yml` (SPA to Cloudflare) on
  green pushes to main; `deploy.yml`'s `workflow_dispatch` re-points ACA at any existing tag (rollback,
  never rebuilds) — **build once, deploy separately**. API deploys before the SPA so the UI never calls an
  endpoint that isn't live. Azure auth is **OIDC**, no stored secret: the token subject
  `repo:…:environment:production` must equal the GitHub Environment name exactly (else `AADSTS70021`).
  Traps: **secrets are not passed to reusable workflows** (`vars` are) — every `uses:` call needs
  `secrets: inherit`, and the symptom is an empty value, not an error; image tags are **short SHAs**,
  which `actions/checkout` won't accept as `ref` (fetch full history with `filter: blob:none`, then
  `git checkout` locally); a `.config/dotnet-tools.json` manifest **takes precedence over a global tool
  install**, so use `dotnet tool restore`; `cache-to: type=gha` needs a `setup-buildx-action` step;
  `GITHUB_TOKEN` pushes only to a GHCR package the repo is linked to with Write; `id-token: write` must be
  on the **calling** job; pin the `docker/*` + `azure/login` majors from the live registry, not memory.
- **Migrations are applied by the pipeline, not at startup (M41).** `Program.cs` gates `Migrate()` on
  `Database:MigrateOnStartup`, **defaulting to `true`** — load-bearing, because the API integration tests
  get their SQLite schema from that call and local `dotnet run` relies on it. Only prod opts out
  (`Database__MigrateOnStartup=false`). CI builds an EF bundle **from the deployed commit** and runs it
  *before* the image swaps, so a failed migration aborts the deploy with the old image still serving.
  Two prerequisites: `ZmgDbContextFactory` (`IDesignTimeDbContextFactory`), or `dotnet ef` boots
  `Program.cs` and demands R2 settings CI has no reason to hold; and `compile` in the EF Design package's
  `IncludeAssets`, which the NuGet default omits so the interface isn't otherwise referenceable.
- **Rolling back the image does not roll back the schema.** EF migrations are forward-only: a bundle built
  at an older tag finds its own migrations already applied and does nothing. Rollback is therefore safe
  only to a tag sharing the current schema, or across **additive-only** migrations — never across a
  destructive one (`DropSoftDelete` is the live example: old code would query dropped columns). Use
  expand/contract if rollback must stay a real safety net.
- **The server stays culture-free — `InvariantGlobalization=true` on plain `chiseled` (M41).** The image
  ships no ICU, so .NET 8 refuses to start without the flag. Safe only because every comparison is
  `Ordinal`/`Invariant` and all string ordering is SQL-side (Postgres collation); **v2.8's i18n kept it
  that way** (codes on the wire, translations as data rows) and anything that introduces a server-side
  `.resx`/`CurrentUICulture` or date/number *format* means switching to `chiseled-extra` in the same
  change. **Verification gap:** the flag lands in the *executable's*
  runtimeconfig, which the test host doesn't inherit, and tests run SQLite — so `dotnet test` never
  exercises Npgsql under invariant mode. Prove it with `docker run` against real Neon on a DB-touching
  endpoint. Escape hatch if a `CultureNotFoundException` ever appears:
  `<PredefinedCulturesOnly>false</PredefinedCulturesOnly>`. Also: no shell in the image, so
  `az containerapp exec` loses bash.
- **Cold start is platform-bound — don't chase it in code (M40/M41).** App boot is ~0.15s; the cold start
  is 16–22s, of which **11–12s is Azure sandbox provisioning** with no knob, plus ingress/KEDA activation.
  The image is re-pulled on **every** cold start (Consumption gives no node affinity), but pull time is
  **latency-bound, not bandwidth-bound** — a 44% smaller image did not shorten it. ReadyToRun (+size on an
  always-pulled image) and an early `fetch('/api/health')` in `index.html` (measured: 160ms of ~20,000ms)
  were both evaluated and cut. Don't re-propose either; the only thing that moved the needle was M42.
- **The Cloudflare Worker is an accelerator, never a dependency (M42).** The container must keep building
  and serving the SPA from `wwwroot` so `docker run` yields a complete app and the ACA URL stays a working
  rollback target — never drop the web stage from the Dockerfile. `run_worker_first: ["/api/*"]` keeps the
  API on the **same origin**, which is why there is no prod CORS policy, no `VITE_API_BASE_URL`, and
  `src/api/client.ts` is untouched. Two build outputs must both keep working: `pnpm build` →
  `../Zmg.Api/wwwroot`, `pnpm build:edge` → `./dist`.
- **Three text layers, three mechanisms (v2.8) — pick the layer before writing the string.** UI chrome is
  i18next JSON bundled into the SPA; API errors/warnings are culture-invariant **codes** the SPA renders;
  checklist task text is **data** (a stable code resolving a per-locale row). README has the per-layer
  steps; the three bullets below are the rules each layer earns.
- **Every user-facing SPA string is an i18next key, and `t()` is typed off `en.json` (M43–M45).** Adding
  a language is a JSON file plus a name in `i18n/language.ts` — never component code.
  `aria-label`/`title`/`placeholder` translate; `KeyboardEvent` keys, `cva` variants and the four
  `ReleaseStatus` wire codes do not.
  - **Never concatenate a sentence** — interpolation, or `_one`/`_other`. Spanish word order differs
    often enough that concatenation is a correctness bug, so pure helpers return **shapes**
    (`{ days: 3 }`) and `hooks/useFormatters` supplies the words.
  - **A missing key is a compile error** (`i18next.d.ts` merges `typeof en`); keys passed around as
    *data* need `ParseKeys`, not `string`. `es.json` is guarded by `i18n/i18n.test.ts` instead — key and
    placeholder parity, no blanks, complete plural families — because types can't see it.
  - **Non-component modules translate off the i18n instance, not a hook.** In `ArtistFormPage`'s load
    effect that's also deliberate: taking `t` as a dep would re-fetch on every language switch.
  - `eslint-plugin-i18next` was evaluated and **not** adopted — it false-positives on Tailwind class
    strings, and the parity test already catches the failure it would.
- **The API ships codes; the SPA owns every user-facing sentence (M46).** `Message(Code, Args?)`, where
  `Code` is an i18next key path 1:1 so rendering is `t(code, args)` with no translation table. The wire is
  `{"errors":[{"code","args"}]}` with **no `message` field** — a parallel prose channel is exactly what
  drifts. `Results.Problem` (500) keeps developer-facing prose; it's the one uncoded string.
  - **Codes are permanent identifiers**, same rule as the integer enums. They live next to the rule that
    raises them (`Validation`, `ReleaseWarnings`, `CoverImage`, `ReleaseMutability`); service-minted ones
    share `Api/Services/ServiceErrors.cs`, since several fire from more than one service.
  - **`PendingAction.Label` is two things, switched on `Kind`** — a task title for `TaskDue` (user
    content, verbatim), a warning code otherwise.
  - **`i18n/serverText.ts` is the only place `ParseKeys` typing gives way** (a code is data at runtime);
    `i18n.exists()` replaces the lost safety and degrades to the raw code rather than a blank. Two entry
    points on purpose: the module instance for `api/client.ts` (which builds `ApiError` outside any React
    tree), a `useServerText()` hook for components — codes arriving as data must re-render on a switch.
  - **`MessageCodeApiTests` is the guard that matters**: every code constant must have a key in *both*
    locales. A code with no key renders as its own key path, in both languages, with everything green.
- **A checklist task's identity is its `Code`, never its title (M47–M48).** `TemplateTask.Code`, stamped
  onto `ReleaseTask.SourceCode` by `TemplateCopy`; **null for user-added tasks**, which are never
  translated. Renaming a code orphans every translation row and every already-stamped release task.
  - **Never key a rule off a title.** `Release.IsDistributed` uses the code — matching the English
    `"Distribute to DSPs"` meant one Spanish title would take the UPC warning, the pending engine and the
    past-date backfill down together, **silently**.
  - **Translation is lookup, never a rewrite.** `TemplateTaskTranslation(TemplateTaskId, Locale, Text)` —
    a child table, not `jsonb`, because tests run SQLite. English *is* the `Title` column, so `en` has no
    rows and every miss (null code, unknown locale, absent row, blank text) falls back to it via pure
    `TaskText.Resolve`. It must never render a raw slug.
  - **A title edit lands in the locale being read, measured against what the user was *shown*.** The SPA
    round-trips the whole editable row, so a phase move sends back the *translated* title; comparing to
    the stored column would overwrite English with Spanish and orphan the code. Template edits fan out to
    **every task sharing the code** (the map is code-keyed and the base list is seeded into both
    templates, so a single-row write appears to do nothing), and text equal to the English title
    **deletes** the row. Release-task edits still null the code — a release owns a snapshot with no
    per-locale text of its own.
  - **Mutation responses resolve too, and a language switch invalidates the query cache** *after*
    `i18n.changeLanguage` — invalidating first refetches the old locale, so the chrome flips and the
    checklist doesn't.
  - **Locale is `X-Lang` → `Accept-Language` → `en`**, resolved by `Ordinal` dictionary lookup, so
    `InvariantGlobalization=true` still holds. `client.ts` must set the header on the FormData branch too.
  - **Untranslated is a valid state, and it's pinned.** Three proper-noun titles (Spotify Canvas / Artist
    Pick / Discovery Mode) get no row at all; `SeedDataTests` asserts that exact set, so a *forgotten*
    translation fails a test instead of passing as English inside a Spanish checklist. Domain jargon
    stays English by rule — DSP/BMI/MLC/SoundExchange/Musixmatch, "smart link", "pre-save", "waterfall",
    "multitracks", "splits", "focus tracks".
- **Prod runs Postgres (Neon); integration tests run SQLite in-memory (v2.5/M30).** Migrations are
  Postgres-specific. Keep query code **provider-agnostic** — e.g.
  title search lowercases both sides of `Like` rather than using Npgsql `ILike` — so SQLite tests stay
  representative. Note Postgres' `lower()` is Unicode-aware while SQLite's is ASCII-only, so accented
  titles may match in prod but not in tests — reachable now that v2.8 put Spanish content in the app.
  Real-Postgres tests (Testcontainers + CI service container) are deferred to Phase 2.

---

## Project layout

```
src/Zmg.Domain   entities/enums, template-copy, progress, status, validation, seed,
                 release-warnings, song-archival, pending-actions  (pure, no I/O)
src/Zmg.Api      minimal API: endpoints, service layer (+ interfaces), DTO contracts, extensions
src/Zmg.Infra    EF Core + Npgsql/Postgres: ZmgDbContext (seeding) + migrations
src/Zmg.Web      React + Vite + Tailwind SPA, organized by feature folder; worker.ts + wrangler.jsonc
                 deploy the edge Worker
tests/Zmg.Domain.Tests   xUnit unit tests
tests/Zmg.Api.Tests      integration tests (WebApplicationFactory + in-memory SQLite)
infra                    Terraform: azurerm + neon + cloudflare in one root module (see infra/README.md)
```

---

## Backlog / next steps

- **Shipped — v2.4 (M26–M28):** UI polish · semantic color tokens · dark/light toggle.
- **Shipped — v2.5 (M29–M34):** ACA deploy · Neon Postgres · R2 covers · cover normalization · Terraform ·
  CI/CD image pipeline.
- **Shipped — v2.6 (M35–M38):** startup fail-fast · hard-delete · responsive navbar · catalog counting.
- **Shipped — v2.7 (M39–M42):** remote Terraform state · cold-start baseline · API boot path ·
  edge-served SPA.
- **Shipped — v2.8 (M43–M48):** i18n foundation + language selector · home/releases strings ·
  catalog/artists/templates strings · API message codes · checklist translations · Spanish content.
- **Next: Phase 2 — DSP stats** (the reason this exists over Notion/Trello): hang streaming/revenue data
  off the stable Artist / Release / **Song** / Track ids and the UPC/ISRC columns; the v2.0 Song ids are
  its foundation. Also real-Postgres tests. No build plan yet — write `build-plan-3.0.md` when it starts.
- **Still open (not gating):** a review pass over the seeded Spanish task titles — corrections go into
  the templates screen, not a migration. Low-value test polish (exhaustive AAA pass, the last few Theory
  conversions); the suite is green without it.
- **Per-track task fan-out** on albums: registrations that repeat per track are single "per track" tasks
  today. Decide after the first real album.
- Deferred: un-archive/restore (archives are terminal by rule); auth for hosted deploys; absolute
  per-task due dates (v1.1 only added timeframe *ranges*); a custom domain in front of the Worker. Also
  carried forward from the M24 audit: the **seed-data 3-way drift hazard** (`SeedData.cs` →
  `InitialCreate` → snapshot, with `DeterministicTaskId` renumbering every later GUID on a mid-list
  insert) — left as-is per CLAUDE.md's hard-reset rule, noted here so Phase 2 doesn't rediscover it.
