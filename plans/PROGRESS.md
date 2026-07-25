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

Newer plan versions go in new `build-plan-N.N.md` files; older ones stay frozen.

**Current state:** feature-complete through **v2.4** and **fully deployed** — **v2.5 (M29–M34) is
complete**. Live on **Azure Container Apps** over **Neon Postgres**, covers in **Cloudflare R2**
(normalized to a 1000px WebP on ingest), the whole stack codified in Terraform under
[`infra/`](../infra/README.md), and a **GitHub Actions pipeline** that builds + pushes on every green
push to main and deploys via OIDC (M34). Backend **domain 119 / API 158**, SPA **32 Vitest** — the
pipeline gates on these. **v2.6 (M35–M38) is complete** — a hardening/cleanup pass before the
multilingual work: startup env-var fail-fast + eager R2 client (M35), hard-delete replacing soft-delete
(M36), responsive hamburger navbar (M37), and catalog release-counting fixes / field collapse (M38); see
[build-plan-2.6.md](build-plan-2.6.md). **v2.7 (M39–M42) is in progress** — infra hardening; **M39 is
done**: Terraform state now lives in an encrypted, versioned Azure Storage container with blob-lease
locking instead of a cleartext file on one laptop. **M40 measured the cold-start baseline** and **M41
items 1–4 cut app boot 2.0s → 0.18s** (migrations moved to the deploy pipeline, chiseled base image);
its re-measure and **M42's edge-served SPA** are what remain — cold start is dominated by Azure platform
latency, so M42 is the milestone that changes what users feel. See
[build-plan-2.7.md](build-plan-2.7.md). **Then v2.8** — multilingual (EN/ES); the M37 language
selector was deliberately deferred there. **Phase 2** (DSP stats, real-Postgres tests) follows and starts
a new `build-plan-3.0.md`.

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

**v2.6 (M35–M38) — hardening.** M35: startup env-var **fail-fast** — a new
`StartupValidationExtensions.Validate(IConfiguration)` gathers *every* missing/blank required key
(`ConnectionStrings__Zmg` + all five `R2__*`) and throws one message naming them all, called right after
`CreateBuilder` (folding in the old connection-string throw). R2 is now **required at startup**, so
`R2StorageService` drops its `Lazy<IAmazonS3>` for a client built eagerly in the constructor. Validation
runs in **every environment including tests** — `ZmgApiFactory` supplies dummy `R2:*` values via
`UseSetting` so the suite boots the same validated path as prod (never dereferenced; `UploadApiFactory`
swaps in the fake storage). M36: **hard-delete replaces soft-delete** — dropped `DeletedAt`, the three
query filters, and the soft-delete model; DELETE now `Remove()`s the row (Release cascades to tasks +
track links; Song `RemoveRange`s its links first past the Restrict FK). `DropSoftDelete` migration ships
the `DROP COLUMN`. Archive (`ArchivedAt`) untouched. Backend **domain 119 / API 158** unchanged. M37:
the inline `Nav`/`ThemeToggle` came out of `App.tsx` into a new `components/NavBar.tsx`. Desktop (≥sm)
keeps the horizontal row; below sm the five links collapse into a `☰` dropdown **sheet** while brand +
theme toggle stay visible. The sheet is a plain `absolute` child of the sticky (untransformed, no
`overflow-hidden`) `z-10` header — no body-portal needed, unlike RowMenu — with a solid `bg-panel` so
links stay readable, closing on route change (`useLocation`) and outside click (a ref check, no overlay).
Verified at 375px + desktop, light + dark, no page-level horizontal scroll. M38: the catalog's
release-counting is now **one source of truth**. Every link-derived value in `ListAsync` excludes
archived links (`releaseCount` too, was counting all — Bug A), and `SongListItemDto` **drops
`isOrphan`/`canArchive`**: the client derives both the three-state **Released** column
(No / Yes / Upcoming) and the Archive action from `ReleaseDate` alone — `null` ⟺ archivable. The
`ArchiveAsync` guard was reduced to its active-release check so the equivalence holds (the old
"already-released" guard 409'd archived-past-release songs the UI offered Archive). Catalog now offers
**Archive only** (Delete moved to Archived Songs); `WithDetailIncludes` includes archived links so the
detail page badges them. Verified in-browser across orphan / upcoming / released rows.

**v2.5 (M29–M34) — deployment.** First hosting: the container image on **Azure Container Apps**
(Consumption, scale-to-zero) (M29); prod off ephemeral SQLite onto **Neon Postgres** via EF Npgsql
(M30); release covers into **Cloudflare R2** through an upload/paste-URL tile that re-stores remote URLs
server-side rather than hotlinking (M31), normalized to a 1000px WebP on ingest (M33); the whole stack
codified in **Terraform** across `azurerm` + `neon` + `cloudflare`, **imported** rather than recreated
so prod never moved (M32); and a **GitHub Actions pipeline** that builds a SHA-tagged image on every
green push to main and deploys to ACA via **OIDC**, with a `workflow_dispatch` rollback to any prior tag
(M34).

**v2.7 (M39) — Terraform state to Azure Storage.** State moved off one laptop into
`zmg-tfstate-rg` / `zmgtfstate1` / `tfstate/zmg.tfstate` — encrypted at rest, versioned, 30-day soft
delete on blobs and containers, and **blob-lease locking** (native, free, and it fires on `plan` as
well as `apply` — verified by racing two plans). Three choices are load-bearing: the state's resource
group is **separate from `zmg-rg`** so a `terraform destroy` can't delete the file describing what it
destroys; **shared key access is disabled on the account**, so no account key exists to leak and the
only way in is an Entra identity with **Storage Blob Data Contributor** (hence `use_azuread_auth` in
the backend block); and the backend was created **by hand with `az`**, since Terraform can't create the
account its own state lives in. Migration was gated on `terraform plan` → *No changes* plus a
`state list` matching all 9 pre-migration resources, after which the local `terraform.tfstate` +
`.backup` were deleted — that's the real win, since they held the Neon connection string, GHCR token
and both R2 keys in cleartext in the working tree. A new `.github/workflows/infra.yml` runs
`fmt -check` + `init -backend=false` + `validate` gated on `paths: ['infra/**']` (separate from
`ci.yml`, which `paths-ignore`s `infra/**`); `-backend=false` is deliberate — CI validates syntax and
never gets access to state, i.e. never gets the secrets. ~$0/mo. No test run: infra + docs only.

**v2.7 (M40) — cold-start baseline.** Permanent `[boot]` timing logs in `Program.cs` (deltas from
Program entry, logged after `builder.Build()`, after the migration step, and on `ApplicationStarted`),
then four measured scale-from-zero starts on **revision `zmg-app--0000008`, image tag `9657702`**:

| Phase | A (post-deploy) | B1 | B2 | B3 |
|---|---|---|---|---|
| `curl` total, client-side | — | 19.72s | 28.09s | 17.71s |
| KEDA activate → pull start | 2.1s | 2.8s | 2.7s | 1.1s |
| image pull | 3.47s | 4.05s | 4.15s | 3.16s |
| pull done → container created | 11.14s | 5.85s | 10.87s | 2.54s |
| container start → listening (app) | 2.02s | 2.03s | 2.11s | 2.05s |
| **scheduled → listening** | **18.94s** | **15.21s** | **20.07s** | **9.04s** |

App internals barely vary: `built` 132–144ms, `DB ready` 1.92–2.01s, `listening` 1.98–2.06s.

**Two of the plan's assumptions were wrong.** (1) **There is no image-cached case** — all three B runs
re-pulled the *same* tag on a fresh node within 20 minutes, because the Consumption profile gives no
node affinity. The A-vs-B comparison M40 was designed around doesn't exist; image size costs on *every*
cold start. (2) **The image is ~91MB compressed** (95,420,416 bytes as reported by ACA; 340MB
uncompressed locally), not the 216MB the plan
assumed — which lowers the ceiling on the chiseled work.

**The split: app boot is 2.0s; everything else is platform.** Of that 2.0s, **1.8s is the DB step**
(Neon wake + a migration check that logged "No migrations were applied" every run). The remaining
14–26s is Azure-side and has no knob: **pull done → container created alone is 2.5–11.1s**, varying 4×
run to run, plus 4.5–8.7s of ingress/activation before KEDA even records the scale event (the gap
between the client `curl` total and the internal timeline).

**Consequences for M41**, since deciding them was the point of M40: items **1–2 (migrations out of
startup) are confirmed** — ~1.8s, i.e. 90% of all app time — with the caveat that this *relocates* the
Neon wake to the first query rather than removing it, so time-to-*listening* drops ~1.8s while
time-to-first-*data* barely moves. Item **3 stays as cleanup, not perf** (it targets the 135ms `built`
phase; worth ~50ms). Item **4 (chiseled) stays, ~1.5s** — weaker than planned on size, stronger in that
nothing is ever cached so it pays out every start. Item **5 (ReadyToRun) is dropped**: only ~200ms of
JIT-sensitive window exists outside the DB step, and +10–15MB on an always-pulled image costs ~0.5s.
**Net M41 ≈ 3.3s off a 17.7–28.1s cold start** — the plan's "→ 10–16s" is not reachable, because there
was only ever 2s of app time to win. Cold start is essentially all platform latency, which makes **M42
(edge-served SPA) the only milestone that fixes what the user experiences.**

**v2.7 (M41) — API boot path.** Items 1–4 shipped; the re-measure (step 5) is pending a deploy of the
chiseled image. **App boot went 2.0s → 0.18s** (`built` 120–136ms, `DB ready` 124–140ms, `listening`
164–192ms), and the DB step went 1.8s → 4ms because it no longer touches the database at all.

**Migrations moved to the deploy pipeline.** `Program.cs` gates `Migrate()` on
`Database:MigrateOnStartup`, **defaulting to `true`** — load-bearing, because `ZmgApiFactory` documents
that the API integration tests get their SQLite schema from that call, and local `dotnet run` relies on
it too. Only prod opts out, via `Database__MigrateOnStartup=false` in `infra/azure.tf`. `deploy.yml`
builds an EF bundle and applies it **before** `az containerapp update`, so a failed migration aborts the
deploy while the old image is still serving. A new `src/Zmg.Infra/Data/ZmgDbContextFactory.cs`
(`IDesignTimeDbContextFactory`) is the prerequisite: without it `dotnet ef` boots `Program.cs`, hits
M35's `Configuration.Validate()`, and demands R2 settings CI has no reason to hold. Referencing
`IDesignTimeDbContextFactory` also required adding `compile` to the EF Design package's `IncludeAssets`
in `Zmg.Infra.csproj` — the NuGet default omits it, so the interface isn't referenceable otherwise.

**Three pipeline gotchas, all now fixed:** image tags are *short* SHAs
([ci.yml](../.github/workflows/ci.yml) uses `type=sha,format=short`), and `actions/checkout` only treats
`ref` as a commit when it's a full 40-char SHA — abbreviated SHAs can't be fetched server-side either, so
the job checks out with `fetch-depth: 0` + `filter: blob:none` (commit graph and trees, no file contents)
and resolves the abbreviation locally with `git checkout`. The repo already pins `dotnet-ef` 8.0.11 in
`.config/dotnet-tools.json`, and that local manifest takes precedence over a global install, so the step
uses `dotnet tool restore`. And **secrets are not passed to reusable workflows automatically** — unlike
`vars`, which is why OIDC worked while `NEON_CONNECTION_STRING` arrived empty; `ci.yml` now calls
`deploy.yml` with `secrets: inherit`.

**Item 3 shrank.** Swagger's `AddEndpointsApiExplorer`/`AddSwaggerGen` (and the dev-only CORS policy)
moved inside `builder.Environment.IsDevelopment()`. The planned lazy `IAmazonS3` was **dropped**:
`R2StorageService` is a singleton, .NET creates singletons on first resolution, `IStorageService` is only
injected into the per-request `CoverUploadService`, and there's no `ValidateOnBuild` — so the S3 client
was never built at boot and `Lazy<T>` would have saved nothing while making M35's comments less accurate.

**Item 4 — chiseled + `InvariantGlobalization`.** `aspnet:8.0` → `aspnet:8.0-noble-chiseled`:
**340MB → 181MB on disk, and 95.5MB → 54.4MB transferred (−43%)** — the transferred figure is the one
that matters, and Docker's "content size" for the live `31c16e4` image (95.5MB) matches ACA's reported
`95,420,416 bytes` exactly, confirming the two measurements are the same thing. Scaling M40's pulls
(3.16–4.15s for 95.4MB) puts the new pull near **1.8–2.4s**, i.e. ~1.3–1.8s saved on every cold start,
since nothing is ever cached. Only the final stage ships — the `node:24-alpine` and `dotnet/sdk:8.0`
build stages are discarded by buildx and never reach GHCR or ACA. Plain chiseled ships no ICU, so .NET 8 needs invariant mode
declared explicitly or it refuses to start. Re-audited before switching: every `string.Equals` is
`OrdinalIgnoreCase`, `CoverImage.cs:55` uses `ToLowerInvariant`, the `.ToLower()` calls in
`SongService`/`ReleaseService` are inside EF expression trees (Postgres `lower()`), and
`PendingActions.cs:108` passes `StringComparer.OrdinalIgnoreCase` explicitly. **Verification gap worth
remembering:** `InvariantGlobalization` lands in the *executable's* runtimeconfig, which the test host
doesn't inherit, and the tests run **SQLite** — so `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test`
never exercises Npgsql. Proven instead with `docker run` against real Neon, hitting `/api/artists` (not
`/api/health`, which touches no database) plus loading the SPA. The `CultureNotFoundException` risk that
gets attributed to EF Core is really `Microsoft.Data.SqlClient`; Npgsql has no such dependency. Escape
hatch if one ever surfaces: `<PredefinedCulturesOnly>false</PredefinedCulturesOnly>`, left at the
default deliberately so anything unexpected fails loudly. Trade-off accepted: no shell in the image, so
`az containerapp exec` loses bash.

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
  CI/CD image pipeline.
- **Shipped — v2.6 (M35–M38):** hardening/cleanup — startup env-var fail-fast + eager R2 client
  (M35), hard-delete replacing soft-delete app-wide (M36), responsive hamburger navbar (M37), and
  catalog release-counting fixes / field collapse (M38). See [build-plan-2.6.md](build-plan-2.6.md).
- **In progress — v2.7 — infra hardening · remote state · cold start (M39–M42).** See
  [build-plan-2.7.md](build-plan-2.7.md). **M39 is done** — Terraform state now lives in an encrypted,
  versioned Azure Storage container with blob-lease locking (~$0/mo), shared-key access disabled, and
  the local cleartext state deleted; `infra.yml` runs `fmt -check` + `validate`. **M40 is done** — the
  cold-start baseline is measured and recorded in the journal above; it found app boot is only **2.0s**
  (1.8s of it the Neon wake + migration check) against 14–26s of untouchable Azure platform latency, and
  that **the image is re-pulled on every cold start** (no node affinity on Consumption). **M41 items 1–4
  are done** — app boot 2.0s → 0.18s, migrations moved to the deploy pipeline behind
  `Database__MigrateOnStartup=false`, Swagger/CORS gated to dev, and the chiseled base image at 340MB →
  181MB uncompressed. **ReadyToRun was cut from the plan outright** as net-negative on an always-pulled
  image. **Next: M41 step 5** — deploy the chiseled image and re-run M40's exact measurement to report
  the end-to-end delta; expect ~3.3s off a 17.7–28.1s cold start, which is why **M42 is where the real
  win is**. **M41** cuts
  the boot path — migrations move to a deploy-time EF bundle, plain `chiseled` base image, dev-only
  Swagger, lazy S3 client. **M42** serves the SPA from a Cloudflare Worker with a same-origin `/api/*`
  proxy, so the UI paints immediately instead of waiting out the container. The plan is written as a
  step-by-step runbook — it's being executed by hand.
- **Then: v2.8 — multilingual (EN/ES).** Layered i18n (react-i18next UI chrome · DB-authored checklist
  translations · API message codes); the language selector deferred from M37 lands here. Outline lives at
  the end of `build-plan-2.7.md`; write `build-plan-2.8.md` when it starts. **Keep the server
  culture-free** — M41 ships `InvariantGlobalization=true`, which is safe only because all three i18n
  layers translate in the browser or the DB.
- **Then: Phase 2 — DSP stats** (the reason this exists over Notion/Trello): hang streaming/revenue data
  off the stable Artist / Release / **Song** / Track ids and the UPC/ISRC columns; the v2.0 Song ids are
  its foundation. Also real-Postgres tests. No build plan yet — write `build-plan-3.0.md` when it starts.
- **Still open (not gating):** Low-value test polish (exhaustive AAA pass, the last few Theory
  conversions). The suite is green without it.
- **Per-track task fan-out** on albums: registrations that repeat per track are single "per track" tasks
  today. Decide after the first real album.
- Deferred: un-archive/restore (archives are terminal by rule); auth for hosted
  deploys; absolute per-task due dates (v1.1 only added timeframe *ranges*). Also carried forward from
  the M24 audit: the **seed-data 3-way drift hazard** (`SeedData.cs` → `InitialCreate` → snapshot, with
  `DeterministicTaskId` renumbering every later GUID on a mid-list insert) — left as-is per CLAUDE.md's
  hard-reset rule, noted here so Phase 2 doesn't rediscover it.
