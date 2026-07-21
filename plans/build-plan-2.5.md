# ZMG Release Tracker — Build Plan v2.5 (deployment)

Delta on [build-plan-2.4.md](build-plan-2.4.md). Continues milestone numbering from M28 → **M29–M32**.

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

**Scope:** the release create/edit flow can either **upload an image** (stored in R2) or **paste an
external URL** (current behavior). `CoverUrl` stays a `string` — **no schema change**; an upload just
produces an R2 public URL stored in the same column.

**Backend (full slice per CLAUDE.md conventions):**
- Add `AWSSDK.S3`. Configure an S3 client for R2: `ServiceURL =
  https://<account>.r2.cloudflarestorage.com`, `ForcePathStyle = true`, region `auto`.
- `Services/IStorageService.cs` + `R2StorageService.cs` (registered in `Program.cs`).
- `Endpoints/UploadEndpoints.cs` → `POST /api/uploads/cover` (multipart): validate content-type
  (png/jpg/webp) and size (~5 MB), key `covers/{guid}{ext}`, `PutObject`, return `{ url }` =
  `PublicBaseUrl + key`. Map in `Program.cs`.
- Config/secrets: `R2:AccountId`, `R2:AccessKeyId`, `R2:SecretAccessKey`, `R2:Bucket`,
  `R2:PublicBaseUrl` — ACA secrets in prod; the **write key stays server-side only**.

**Frontend:**
- `src/api/uploads.ts`: `uploadCover(file)` → multipart POST, returns the URL.
- `features/releases/ReleaseFormPage.tsx`: add a file-upload control beside the existing URL field;
  upload sets `CoverUrl` to the returned R2 URL; URL paste still works. `ReleaseHeader`/`ReleaseCard`
  already render `CoverUrl`.

**R2 setup (manual now, Terraform in M32):** create the bucket, enable public access (`r2.dev` URL),
create a bucket-scoped API token (access key/secret).

**Verification:** `dotnet test` incl. the new endpoint (content-type/size guards; service can be tested
against a MinIO/Testcontainers S3 or a mocked client); SPA — create a release via **upload** (image
persists and renders from R2) and via **URL** (still works); `pnpm lint` + `pnpm build`.

**Files:** `Zmg.Api.csproj`, `Services/IStorageService.cs`+`R2StorageService.cs`,
`Endpoints/UploadEndpoints.cs`, `Program.cs`, `Contracts/Dtos.cs` (upload response), SPA
`api/uploads.ts`, `features/releases/ReleaseFormPage.tsx`.

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
- **Real-Postgres integration tests.** Run the API suite against actual Postgres — Testcontainers
  locally + a GitHub Actions Postgres **service container** in CI — closing the SQLite/Postgres parity
  gap. (A Testcontainers-in-CI hang during M30 made this not worth blocking on; the factory already has
  the env-var branch pattern sketched for it.)
- **CI/CD image pipeline.** Automate the manual `docker build`/`push`/`az containerapp update` on push
  to main via `docker/login-action` + `docker/metadata-action` + `docker/build-push-action` — SHA tags
  per commit, semver tags on git-tag/release, `GITHUB_TOKEN` auth (no PAT). Optionally a CD step running
  `az containerapp update` with the fresh tag.
