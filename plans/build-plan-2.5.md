# ZMG Release Tracker — Build Plan v2.5 (deployment)

Delta on [build-plan-2.4.md](build-plan-2.4.md). Continues milestone numbering from M28 → **M29–M33**.

## Context

The app is feature-complete through v2.4 and today runs as a **single container** (React SPA served
from `wwwroot`) over **SQLite** on a local volume. The goal of v2.5 is to get it **hosted for
$0–~$1/mo** on a stack that fits .NET/EF and leaves room for phase-2 work (DSP stats aggregation,
first-class image storage). After research, the target stack is:

- **Compute:** Azure Container Apps (Consumption, scale-to-zero) — runs the existing container image.
- **Database:** Neon Postgres (free standing tier) via EF Core Npgsql.
- **Image storage:** Cloudflare R2 (10 GB, zero egress), S3-compatible.
- **IaC:** Terraform spanning `azurerm` + `neon` + `cloudflare` in one config.

Decisions locked with the user:
- ACA runs **scale-to-zero as-is** — no warm-keeping / `min-replicas`. Cold-start tuning is Phase 2.
- Integration tests **stay on SQLite in-memory** (migrations are now
  Postgres-only), search kept provider-agnostic with `LOWER()` so SQLite runs stay valid. Real-Postgres
  tests (Testcontainers + a CI Postgres service) are **deferred to Phase 2** (a CI Testcontainers hang
  made it not worth blocking M30).
- R2 supports **both** direct upload **and** URL paste. No custom domain — use the bucket's public
  `r2.dev` URL for now.
- **Terraform is the last milestone** — it codifies infra that M29–M31 stand up manually first
  (learn-then-codify).

Blast radius (per CLAUDE.md): **M29** infra-only. **M30/M31** touch API/DTO/migration → full
`dotnet test`. **M32** IaC-only, no app code.

---

## M29 — Containerize & deploy the current app to ACA (as-is)

**Scope:** prove the existing image runs unchanged on Azure Container Apps. Still SQLite, still one
container serving the SPA from `wwwroot`.

**Key point:** ACA's filesystem is **ephemeral** → the SQLite file resets on every restart/scale-to-zero.
That's acceptable here — this milestone validates the deploy path only; persistence arrives in M30.

**Steps:**
- Build & push the image to a registry — **Azure Container Registry (Basic)** preferred (keeps it
  in-Azure for M32 + faster same-region pulls); GHCR works too.
- Create a resource group, a Log Analytics workspace (ACA requires one), and an ACA **environment**
  (Consumption).
- Create the **Container App** from the image: external ingress, **target port 8080** (matches the
  Dockerfile `EXPOSE`/`ASPNETCORE_URLS`), `min-replicas 0`, `max-replicas 1`.
- Env: keep the Dockerfile default `ConnectionStrings__Zmg=Data Source=/data/zmg.db` (ephemeral, fine)
  and `ASPNETCORE_ENVIRONMENT=Production`. Startup `Migrate()` seeds a fresh db on each cold start.
- Grant ACA pull access to the registry (managed identity or admin creds).

**Verification:** open the ACA URL → SPA loads, `/api/health` returns ok, creating an artist/release
works; confirm data resets after a manual restart (expected). No repo/code/test changes.

**Files:** none in-repo (infra only). Dockerfile unchanged.

---

## M30 — Swap SQLite → Neon Postgres (Npgsql)

**Scope:** replace the EF provider, regenerate migrations for Postgres, preserve search behavior, and
keep integration tests on SQLite in-memory.

**Semantics (mostly clean — audited):**
- Timestamps already use `DateTime.UtcNow` everywhere (e.g. `ReleaseService.cs:313`,
  `SongService.cs:152`) → Npgsql `timestamptz` maps with no code change.
- Dates are `DateOnly` → `date`; keys are `Guid` → `uuid`. No identity/serial columns.
- Case-insensitive **equality** is in-memory (`StringComparer.OrdinalIgnoreCase`, `Validation.cs`) →
  portable.
- **The one behavioral change:** title search uses `EF.Functions.Like` (`ReleaseService.cs:44`,
  `SongService.cs:33`). SQLite `LIKE` is case-insensitive; Postgres `LIKE` is case-sensitive → keep it
  **provider-agnostic** by lowercasing both sides:
  `EF.Functions.Like(x.Title.ToLower(), $"%{term.ToLower()}%")` — case-insensitive on both, no
  Npgsql-specific `ILike`.

**Backend changes:**
- `src/Zmg.Api/Zmg.Api.csproj`: drop `Microsoft.EntityFrameworkCore.Sqlite`, add
  `Npgsql.EntityFrameworkCore.PostgreSQL` (keep the Design package).
- `Program.cs:14`: `UseSqlite` → `UseNpgsql`.
- `ReleaseService.cs:44` + `SongService.cs:33`: lowercase both sides of the `Like` (portable, no `ILike`).
- Migrations: delete the SQLite `InitialCreate` under `src/Zmg.Infra/Migrations/`, regenerate with
  `dotnet ef migrations add InitialCreate` (Npgsql). The `HasData` seed (`ZmgDbContext.cs:110-127`)
  travels automatically.
- Connection string: Neon **pooled** string with `sslmode=require`. Dev → `appsettings.Development.json`
  (a Neon dev branch or local Postgres); prod → `ConnectionStrings__Zmg` as an ACA secret.

**Tests (SQLite in-memory):**
- Keep `ZmgApiFactory` on the shared open SQLite in-memory connection (one isolated DB per factory).
- Parity gap accepted: SQLite won't exercise Postgres type-mapping; that's covered by the real
  migration applying cleanly to Neon + the live deploy. Real-Postgres tests are Phase 2.

**Neon setup (manual now, Terraform in M32):** create the project + main branch (prod) and optionally a
dev branch; copy the pooled connection string.

**Verification:** full `dotnet test` green (SQLite in-memory); run the API against the Neon dev
branch and confirm **case-insensitive search** returns mixed-case matches; redeploy the ACA app with
`ConnectionStrings__Zmg` → data now **persists** across restarts. Update PROGRESS/CLAUDE notes (the
`rm zmg.db` reset instruction is now Postgres-based).

**Files:** `Zmg.Api.csproj`, `Program.cs`, `ReleaseService.cs`, `SongService.cs`,
`src/Zmg.Infra/Migrations/*` (regenerated), `tests/Zmg.Api.Tests/ZmgApiFactory.cs` + test csproj.

---

## M31 — Cloudflare R2 for cover images (upload + URL)

**Scope:** the release create/edit flow sets the cover by **uploading an image** or **pasting an image
URL** — **both stored in R2** (the URL path is server-side fetched, then uploaded, so no external
hotlinks). `CoverUrl` stays a `string` (an R2 public URL) — **no schema change**. UI is the compact
**tile** control (agreed via mockups): empty tile + a "paste a URL" link that reveals an inline input;
once set, the tile becomes the thumbnail with **Replace** / **Remove**; identical in create and edit.

**Backend (full slice per CLAUDE.md conventions):**
- Add `AWSSDK.S3`. Configure an S3 client for R2: `ServiceURL =
  https://<account>.r2.cloudflarestorage.com`, `ForcePathStyle = true`, region `auto`.
- `Services/IStorageService.cs` + `R2StorageService.cs` (registered in `Program.cs`).
- `Endpoints/UploadEndpoints.cs` (mapped in `Program.cs`) — two ingest paths that both `PutObject` to
  `covers/{guid}{ext}` and return `{ url }` = `PublicBaseUrl + key`:
  - `POST /api/uploads/cover` (multipart file) — validate content-type (png/jpg/webp) + size (~5 MB).
  - `POST /api/uploads/cover-from-url` (`{ url }`) — server **fetches** the remote image, then stores
    it. **SSRF guards:** http/https only, block private/loopback/link-local/metadata IPs, cap
    redirects, timeout, cap download size; re-check content-type + magic bytes.
- Config/secrets: `R2:AccountId`, `R2:AccessKeyId`, `R2:SecretAccessKey`, `R2:Bucket`,
  `R2:PublicBaseUrl` — ACA secrets in prod; the **write key stays server-side only**.

**Frontend:**
- `src/api/uploads.ts`: `uploadCover(file)` and `uploadCoverFromUrl(url)` → both return the R2 URL.
- New `features/releases/components/CoverField.tsx` (the tile control): empty tile (click = file
  picker) + a "paste an image URL" link that reveals an inline input; **uploading** = spinner on the
  tile; **filled** = thumbnail + Replace/Remove; **error** = red hint, form stays usable. Client guard:
  reject non-image / >5 MB before POST. Sets the form's `coverUrl` to the returned R2 URL (or null on
  Remove). Replaces the current Cover URL `<Field>` in `ReleaseFormPage.tsx`.
- `ReleaseHeader`/`ReleaseCard` already render `CoverUrl` — no change.

**R2 setup (manual now, Terraform in M32):** create the bucket, enable public access (`r2.dev` URL),
create a bucket-scoped API token (access key/secret).

**Verification:** `dotnet test` — both endpoints (content-type/size guards; **SSRF guards** on the URL
fetch: rejects private/loopback hosts + non-image); storage service mocked or against a MinIO/S3 test
double. SPA — create a release via **upload** and via **URL** (both persist + render from R2), plus
Replace/Remove and edit-mode load; `pnpm lint` + `pnpm build`.

**Files:** `Zmg.Api.csproj`, `Services/IStorageService.cs`+`R2StorageService.cs`,
`Endpoints/UploadEndpoints.cs`, `Program.cs`, `Contracts/Dtos.cs` (upload response), SPA
`api/uploads.ts`, `features/releases/components/CoverField.tsx` (new),
`features/releases/ReleaseFormPage.tsx`. Deferred: deleting orphaned R2 objects on replace/remove.

---

## M32 — Terraform (multi-provider IaC)

**Scope:** codify M29–M31 as one Terraform config across three providers, and answer "how does
multi-provider work."

**How multi-provider Terraform works:** one root module declares multiple `provider` blocks —
`azurerm`, `neon`, `cloudflare`. Each is an independent plugin; a single `terraform apply` builds one
dependency graph across all three and keeps **one unified state**. Resources cross-reference via
outputs — e.g. the **Neon connection string** and **R2 credentials** feed straight into the ACA
container app's secrets. No glue needed; Terraform resolves ordering.

**Resources:**
- `azurerm`: resource group, Log Analytics workspace, Container Apps environment, Container App
  (ingress 8080, min 0/max 1, image ref), app **secrets**, ACR (or reference GHCR).
- `neon`: project + branch + database + role → output the pooled connection string.
- `cloudflare`: R2 bucket (+ public access) and a bucket-scoped token (or pass a pre-made token as a var).

**Wiring:** Neon conn-string output → ACA secret `ConnectionStrings__Zmg`; R2 outputs → ACA secrets
`R2__*`; R2 public base URL → env.

**Secrets/state:** sensitive values (Neon password, R2 secret, provider creds) via a **gitignored
`terraform.tfvars`** or environment (`ARM_*`, `CLOUDFLARE_API_TOKEN`, `NEON_API_KEY`); mark outputs
sensitive. State backend: local (gitignored) to start, or an Azure Storage remote backend later.

**Layout:** `infra/` — `providers.tf`, `azure.tf`/`neon.tf`/`cloudflare.tf`, `variables.tf`,
`outputs.tf`, `terraform.tfvars.example`.

**Verification:** `terraform init/plan/apply` into a fresh/renamed resource group → matches what was
built by hand in M29–M31; app reachable, DB connected, uploads work; `terraform destroy` tears down
cleanly.

**Files:** new `infra/*.tf`; `.gitignore` (tfvars, `.terraform/`, state); README pointer.

---

## M33 — Normalize covers on ingest (resize + re-encode)

**Why:** M31 stores whatever it's given. A 4 MB phone photo lands in R2 at 4 MB and gets rendered at
96px on a tile. These covers are **reference images, not artwork masters** — nothing downstream needs
the original resolution. Shrinking them also shrinks the blast radius of the orphan problem below: at
~50 KB an abandoned upload stops being worth engineering around.

**Scope:** every accepted image is decoded, downscaled and re-encoded to **WebP** before it reaches
R2. Applies to **both** ingest paths (upload and URL fetch), since both funnel through
`CoverUploadService.StoreAsync`.

**Decisions:**
- **Library: `SixLabors.ImageSharp`, pinned to `3.1.x`.** It is the only mature **fully managed**
  option — SkiaSharp/Magick.NET/NetVips all ship native binaries, which fights the chiseled-base-image
  goal in Phase 2. Pinned to 3.x deliberately: **v4.0.0 added build-time licence enforcement**
  (a `sixlabors.lic` file must be present to compile), which would break the Dockerfile and CI even
  though this project qualifies for a free licence under the Split License (open-source / <$1M
  revenue). Do **not** let it float to 4.x. Fallback if the licence ever binds: 2.1.x is plain Apache-2.0.
- **Bounds:** longest edge **1000px** (never upscale), WebP quality **80** → typically 30–80 KB.
- **`FileFormat` must be set to `Lossy` explicitly.** Left at the default, ImageSharp's `WebpEncoder`
  can emit **lossless** WebP (`VP8L`), where `Quality` is meaningless — measured live, a 4.3 MB source
  came back at 2.9 MB (vs 584 KB lossy). Silent, and it defeats the entire milestone.
- **Always re-encode**, even for an already-small input. The consistency is the point (see below), and
  the quality cost on a cover thumbnail is invisible.
- The 5 MB cap stays as an **ingress** limit; it now bounds what we're willing to *decode*, not what
  we store.

**Three benefits beyond size** (the reason this isn't just a nice-to-have):
- **Strips EXIF** — phone photos carry GPS coordinates. Metadata profiles are cleared explicitly,
  *after* `AutoOrient()` applies the orientation tag (clear first and portrait photos store sideways).
- **Neutralizes malformed-image payloads.** A file crafted against an image-parser bug doesn't survive
  a decode/re-encode round trip, so it never reaches R2 to be served to a browser. This is a stronger
  control than the magic-byte sniff, which only checks the first few bytes.
- **One stored type.** Everything in the bucket is `.webp`, so the key/extension logic collapses.

**Layering:** the numbers (`MaxEdgeAllocated`, quality, stored content type) go in pure `CoverImage`
alongside the other rules; the ImageSharp call lives in `Zmg.Api` (`Services/CoverProcessor.cs`) —
**Domain stays free of third-party dependencies** per CLAUDE.md. Order inside `StoreAsync` matters:
sniff **first** (cheap reject before handing attacker bytes to a decoder), then normalize, then upload.

**Tests:** the existing API upload tests currently post byte *headers* rather than real images — they
must become genuinely valid images (ImageSharp arrives transitively via `Zmg.Api`, so no new test
package). New coverage: a 1200×800 PNG is stored as WebP with its longest edge ≤1000; a small image
is not upscaled; a decodable-but-corrupt file is rejected as a 400 rather than throwing. The
cap/sniff tests keep working unchanged, since both reject before the decode step.

**Files:** `Zmg.Api.csproj`, `src/Zmg.Domain/CoverImage.cs`, `src/Zmg.Api/Services/CoverProcessor.cs`
(new), `CoverUploadService.cs`, `tests/Zmg.Api.Tests/UploadApiTests.cs`. Blast radius: API →
full `dotnet test`. No SPA change (the tile already renders whatever URL comes back).

---

## Phase 2 — deferred (not scheduled)

- **SPA → Cloudflare Pages split.** Move the React app off the API container to Pages (global CDN,
  instant shell load) so cold starts only delay the first data call. Requires: drop the Dockerfile SPA
  stage + static-file middleware (`Program.cs:59-61`), enable **prod CORS** for the Pages origin
  (`Program.cs:24-28`, currently dev-only), add `VITE_API_BASE_URL` + absolute base in `api/client.ts`,
  and a second deploy. Biggest perceived cold-start win.
- **Cold-start / image tuning.** Move `db.Database.Migrate()` off startup (`Program.cs:36-40`) into a
  deploy-time ACA Job; chiseled runtime base image; `PublishReadyToRun`; EF compiled model
  (`dotnet ef dbcontext optimize`); `InvariantGlobalization` (verify date formatting). Target
  ~20–30s → ~5–8s cold start.
- **Background aggregation (DSP stats).** A nightly **ACA cron Job** pulling third-party API data into
  Neon; parallelize external calls to keep billed wall-clock time short. Add an Azure Storage Queue
  only if bursty on-demand fan-out appears. Draws from the same ACA free grant.
- **Orphaned-cover sweeper (second ACA cron Job).** M31 stores a cover the moment it's picked, so R2
  accumulates objects the app no longer references: abandoned create forms, and every Replace/Remove
  (both only mutate form state — the release keeps pointing at the old URL until save). One
  reconciliation job handles every source: list `covers/`, delete any key not referenced by a
  `CoverUrl` in the database, **skipping objects younger than ~24h** so an in-flight form can't be
  swept out from under itself. Soft-deleted releases still carry their `CoverUrl`, so the
  "still referenced" query protects them for free.
  **Explicitly rejected: deleting eagerly when Replace/Remove is clicked.** Those buttons only change
  local form state — deleting there, followed by the user hitting Cancel or closing the tab, breaks
  the cover of a release that was never edited. Any deletion must key off what was *persisted*.
  The alternative considered and parked: upload to a `staging/` prefix, `CopyObject` to `covers/` on
  save, and let an **R2 lifecycle rule** expire the staging prefix (deletion as configuration, no code)
   — rejected for now because it couples the promote step into `ReleaseService`, and M33 makes each
  orphan ~50 KB. Cheap to add here since the DSP job above already brings the Job runner.
- **Real-Postgres integration tests.** Run the API suite against actual Postgres — Testcontainers
  locally + a GitHub Actions Postgres **service container** in CI — closing the SQLite/Postgres parity
  gap. (A Testcontainers-in-CI hang during M30 made this not worth blocking on; the factory already has
  the env-var branch pattern sketched for it.)
- **CI/CD image pipeline.** Automate the manual `docker build`/`push`/`az containerapp update` on push
  to main via `docker/login-action` + `docker/metadata-action` + `docker/build-push-action` — SHA tags
  per commit, semver tags on git-tag/release, `GITHUB_TOKEN` auth (no PAT). Optionally a CD step running
  `az containerapp update` with the fresh tag.
