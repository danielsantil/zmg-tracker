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
- [build-plan-2.8.md](build-plan-2.8.md) — multilingual EN/ES (M43–M48). **In progress: M43–M47 done.**

Newer plan versions go in new `build-plan-N.N.md` files; older ones stay frozen.

**Current state:** feature-complete through **v2.4** and **fully deployed**, shipped through **v2.7
(M39–M42)**. The SPA serves from a **Cloudflare Worker** at the edge with `/api/*` proxied same-origin
to **Azure Container Apps** over **Neon Postgres**; covers live in **Cloudflare R2**; the hosted stack is
codified in Terraform under [`infra/`](../infra/README.md), with remote state in Azure Storage. A
**GitHub Actions pipeline** tests, builds a SHA-tagged image, applies migrations, deploys to ACA over
OIDC, then ships the SPA to Cloudflare. Backend **domain 134 / API 213** (119/158 before v2.8), SPA **50
Vitest** (32 before v2.8) — the pipeline gates on these. **Phase 2** (DSP stats, real-Postgres tests)
follows v2.8 and starts a new `build-plan-3.0.md`.

> 🚧 **v2.8 is in flight on `feat/i18n-multilingual`, branched off `dev` — not merged, not deployed.**
> M43–M47 are committed and pushed: the SPA is fully EN/ES, the API ships **codes, not prose**, and
> checklist text now resolves **per locale off a stable task code**. **M48 is next** — it seeds the
> Spanish task text and is the one milestone that **needs the user** (a review pass over the 41 titles).
> Everything the next session needs is in [build-plan-2.8.md](build-plan-2.8.md), ticked through M47.
> **M47 added a migration** (`TaskCodesAndTranslations`) — the first of v2.8, additive-only, already
> applied to the dev Neon branch; prod gets it through the pipeline's EF bundle on merge.

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

**v2.8 (M43–M48) — multilingual EN/ES. IN PROGRESS: M43–M47 shipped on `feat/i18n-multilingual`.**
react-i18next wired with both locales bundled (no HTTP backend — a round-trip would undo M42's edge
first paint), language state mirroring `useTheme` including its persist-only-on-explicit-choice rule,
`<html lang>` stamped pre-paint, and the language selector deferred from M37 finally in the navbar
(M43); then ~270 UI strings migrated feature by feature — home + releases + the shared components they
render (M44), catalog + artists + templates (M45). Web tests **32 → 50**. The design decision that
shaped everything: `t()` is **typed off `en.json`**, so a missing key is a compile error, and pure
helpers return *shapes* (`{ days: 3 }`) rather than sentences, because plural forms and word order
differ per language. Then **M46 took the prose off the wire entirely**: every server-minted sentence —
42 of them, across validation errors, release warnings and pending-action labels — became a
culture-invariant `Message` code the SPA renders, which is what lets the container keep
`InvariantGlobalization=true` while the user reads Spanish. API tests **158 → 204**. Then **M47** gave
the checklist itself a per-locale identity: a stable `TaskCodes` slug on every seeded task
(`TemplateTask.Code`, stamped onto `ReleaseTask.SourceCode` at copy time), a
`TemplateTaskTranslation` child table, and `X-Lang` resolution at read time — so translating never
rewrites a row and a user's edit is never reverted by a language switch. It also fixed the latent bug
the plan flagged: `Release.IsDistributed` matched the literal English `"Distribute to DSPs"`, so one
Spanish title would have silently stopped the UPC warning, the pending engine and the past-date
backfill. Tests **domain 119 → 134, API 204 → 213**. **Still English:** the Spanish task text itself
(M48 seeds it).

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
  `Ordinal`/`Invariant` and all string ordering is SQL-side (Postgres collation); **v2.8's i18n must keep
  it that way** — any server-side `.resx`/`CurrentUICulture` or date/number *formatting* means switching
  to `chiseled-extra` in the same change. **Verification gap:** the flag lands in the *executable's*
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
- **Every user-facing SPA string is an i18next key, and `t()` is typed off `en.json` (v2.8/M43–M45).**
  One namespace, nested keys under `src/i18n/locales/{en,es}.json`, both **bundled** into the JS. Adding
  a language = adding a JSON file plus a name in `i18n/language.ts` — never touching component code.
  Four rules that aren't obvious from the code:
  - **Never concatenate a sentence.** `t('x', { count })` with `_one`/`_other`, or interpolation — Spanish
    word order differs often enough that concatenation is a correctness bug, not a style one. Pure
    helpers (`lib/format`, `lib/calendar`) therefore return **shapes** (`{ days: 3 }`,
    `{ kind: 'range', … }`) and `hooks/useFormatters` supplies the words; `Intl` owns date wording via a
    `locale` argument.
  - **A missing key is a compile error** (`i18n/i18next.d.ts` merges `typeof en` into `CustomTypeOptions`).
    Keys passed around as data need the `ParseKeys` type, not `string` — see `ReleaseFormPage`'s
    `validateForm`, which returns keys so it stays pure.
  - **`es.json` is guarded by `i18n/i18n.test.ts`**, not by types: key parity, placeholder parity, no
    blanks, complete plural families. It is the thing that catches a key added to `en` and forgotten in
    `es`. Vitest is `environment: 'node'` over `*.test.ts` only — no Testing Library, so i18n tests stay
    pure-module.
  - **Non-component modules translate off the i18n instance** (`import i18n from '@/i18n'`), not a hook —
    `releases/archiveConfirm.tsx` and `ArtistFormPage`'s load effect. In the effect's case that's also
    deliberate: taking `t` as a dep would re-fetch the artist on every language switch.
  `aria-label`/`title`/`placeholder` translate; `KeyboardEvent` keys, `cva` variant keys, and the four
  `ReleaseStatus` wire codes do not. `eslint-plugin-i18next` was evaluated and **not** adopted — it
  false-positives on Tailwind class strings, and the parity test catches the failure it would.
- **The API ships codes; the SPA owns every user-facing sentence (v2.8/M46).** No server-minted prose
  reaches a user. `Zmg.Domain.Message(Code, Args?)` is the unit — `Code` is an i18next key path 1:1, so
  rendering is `t(code, args)` with no translation table, and `args` values are already-formatted
  strings so no culture is needed to produce them. The wire is `{"errors":[{"code","args"}]}` with
  **no `message` field**: a parallel prose channel is exactly what drifts (same reason there are only
  two warning channels). A raw `curl` gets a code instead of a sentence — acceptable with one consumer,
  and better in logs. Five things that aren't obvious:
  - **Codes are permanent identifiers.** Renaming one breaks both sides at once, same rule as the
    integer enums. They live next to the rule that raises them (`Validation.*`,
    `ReleaseWarnings.*`, `ReleaseMutability.ArchivedReadOnlyCode`, `CoverImage.*Code`) — except the
    service-minted ones, which need the DB to detect and share one home in `Api/Services/ServiceErrors.cs`
    because several fire from more than one service.
  - **`Results.Problem` (500) keeps its prose.** It's developer-facing, rides in `Message.Code` by
    convention, and is the one string on the wire M46 deliberately doesn't code.
  - **`PendingActionDto.Label` is two things, switched on `Kind`** — a task *title* for `TaskDue` (user
    content, verbatim) and a warning *code* for the three data kinds. No DTO change; the SPA branches.
  - **`i18n/serverText.ts` is the only place `ParseKeys` typing gives way**, because a code is data at
    runtime. `i18n.exists()` replaces the lost type safety and degrades to showing the raw code rather
    than a blank if the API ever deploys ahead of the SPA. It has two entry points on purpose:
    `translateMessage` off the module instance for `api/client.ts` (which builds `ApiError` outside any
    React tree, translating at construction so `errorMessage(e, fallback)` is unchanged everywhere),
    and a `useServerText()` hook bound to `useTranslation`'s `t` for components — codes arriving as
    *data* must re-render on a language switch, and the module instance wouldn't.
  - **`MessageCodeApiTests` is the guard that matters**: it reflects over every code constant in both
    projects and asserts each has a key in `en.json` *and* `es.json`. A code with no key renders as its
    own key path, in both languages, with every other test green — nothing else catches that.
- **A checklist task's identity is its `Code`, never its title (v2.8/M47).** `TaskCodes` holds a stable
  slug per seeded task; `TemplateTask.Code` carries it, `TemplateCopy` stamps it onto
  `ReleaseTask.SourceCode`, and both are **null for user-added tasks** — which is correct, since those
  are user content and are never translated. Codes are permanent identifiers: renaming one orphans
  every translation row and every already-stamped release task. Consequences that bit, or would have:
  - **`Release.IsDistributed` keys off `SourceCode`**, not `Title` (plus the two `ReleaseService`
    comparisons). Matching English prose meant one Spanish title would have taken the UPC warning, the
    pending engine and the past-date backfill down together, **silently**. Any future "is this the X
    task?" rule keys off the code — never the title.
  - **Translation is lookup, never a rewrite.** `TemplateTaskTranslation(TemplateTaskId, Locale, Text)`
    — a child table, not `jsonb`, because tests run SQLite. **English is the `Title` column**, so `en`
    has no rows and every miss (null code, unknown locale, absent row, blank text) falls back to it via
    pure `TaskText.Resolve`. It must never render a raw slug.
  - **A title "edit" is measured against the text the user was *shown*.** The SPA round-trips the whole
    editable row, so a phase move sends back the *translated* title; comparing it to the stored English
    column would read that as an edit, overwrite the column with Spanish and orphan the code, for every
    moved task. Both update paths compare against `TaskText.Resolve(...)`, and a real edit stores the
    new text and nulls the code.
  - **Mutation responses resolve too, and a language switch invalidates the query cache.** The task
    hooks replace their local row with the server's DTO, so an English echo would flip a title mid-list
    on every toggle; and cached payloads are in the *previous* language after a switch, so
    `useLanguage` calls `queryClient.invalidateQueries()`.
  - **Locale comes from `X-Lang`** (set by `client.ts` on both the JSON and FormData branches), then
    `Accept-Language`, then `en`. Resolution is `Ordinal` string matching over a dictionary — no
    `CultureInfo` anywhere, so `InvariantGlobalization=true` still holds.
- **Prod runs Postgres (Neon); integration tests run SQLite in-memory (v2.5/M30).** Migrations are
  Postgres-specific. Keep query code **provider-agnostic** — e.g.
  title search lowercases both sides of `Like` rather than using Npgsql `ILike` — so SQLite tests stay
  representative. Note Postgres' `lower()` is Unicode-aware while SQLite's is ASCII-only, so accented
  titles may match in prod but not in tests — this starts mattering with v2.8's Spanish content.
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
- **In progress — v2.8 (M43–M48): multilingual EN/ES**, on `feat/i18n-multilingual` off `dev`.
  **Done:** M43 i18n foundation + language selector · M44 home/releases strings · M45
  catalog/artists/templates strings · M46 API messages as stable codes · M47 DB-authored checklist
  translations (schema, resolution, and the `IsDistributed` fix). The SPA chrome is fully bilingual, the
  API ships no prose, the checklist mechanism is proven with English-only rows, and each milestone is
  its own pushed commit.
  **Next up — M48: Spanish checklist content + per-locale editing.** Read
  [build-plan-2.8.md](build-plan-2.8.md) first; its checklists are ticked through M47. Three things to
  know going in:
  - **M48 needs the user**, and only for reviewing the 41 Spanish task titles. Per the user (v2.8 kickoff)
    the bar is a first pass, **not** perfect copy: leave anything genuinely ambiguous in English rather
    than guessing, and they'll finish it on the running site. The domain jargon
    (DSP/BMI/MLC/SoundExchange/Musixmatch/Canvas/Artist Pick, "smart link", "pre-save", "waterfall",
    "multitracks") stays English on purpose.
  - **The rows seed through `SeedData` + `HasData`**, keyed `(TemplateTaskId, Locale)` — deterministic
    ids already, so none of the `DeterministicTaskId` renumbering hazard applies. The base checklist is
    seeded into *both* templates, so each shared title needs a row per template task (72 rows for 41
    titles), translated once in the source.
  - **Step 4 (the templates editor's per-locale field) is explicitly droppable.** Seeded Spanish with no
    in-app editor is a complete, coherent state; the editor is convenience, not correctness. If M48 runs
    long, ship steps 1–3 + 5 and carry step 4 into the backlog.
  Keep the server culture-free throughout — see Cross-cutting decisions. Nothing in v2.8 needs infra,
  secrets, or money.
- **Then: Phase 2 — DSP stats** (the reason this exists over Notion/Trello): hang streaming/revenue data
  off the stable Artist / Release / **Song** / Track ids and the UPC/ISRC columns; the v2.0 Song ids are
  its foundation. Also real-Postgres tests. No build plan yet — write `build-plan-3.0.md` when it starts.
- **Still open (not gating):** Low-value test polish (exhaustive AAA pass, the last few Theory
  conversions). The suite is green without it.
- **Per-track task fan-out** on albums: registrations that repeat per track are single "per track" tasks
  today. Decide after the first real album.
- Deferred: un-archive/restore (archives are terminal by rule); auth for hosted deploys; absolute
  per-task due dates (v1.1 only added timeframe *ranges*); a custom domain in front of the Worker. Also
  carried forward from the M24 audit: the **seed-data 3-way drift hazard** (`SeedData.cs` →
  `InitialCreate` → snapshot, with `DeterministicTaskId` renumbering every later GUID on a mid-list
  insert) — left as-is per CLAUDE.md's hard-reset rule, noted here so Phase 2 doesn't rediscover it.
