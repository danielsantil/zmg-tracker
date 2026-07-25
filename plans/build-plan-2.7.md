# ZMG Release Tracker — Build Plan v2.7 (infra hardening · remote state · cold start)

Delta on [build-plan-2.6.md](build-plan-2.6.md). Continues milestone numbering from M38 → **M39–M42**.

## Context

Two problems:

1. **Terraform state is a local file on one machine.** `infra/terraform.tfstate` holds the live Neon
   connection string, GHCR token, and both R2 keys in plaintext — no locking, no history, no backup.
2. **Cold start is ~17–25s.** The API does avoidable work before it starts listening, and it also serves
   the SPA — so the whole cold start is dead time in front of a blank page.

## How this plan is run

The user drives one milestone at a time ("let's do M40") and **executes the infra/CLI steps themselves**.

**Walk them through interactively — one step at a time, explaining as you go. Do not paste this file at
them.** They're not deep in infra/CI/performance tuning, so explain what a command does and what a good
result looks like before they run it. This file is your reference, not their reading material.

- **Terminal (you)** = the user runs it. **Code change** = you edit the repo.
- **Order matters: M40 before M41** — its numbers decided M41's scope (they kept item 4 and cut ReadyToRun).
- Milestones are independently shippable; stopping after any one leaves the project in a good state.
- Checklists are the resume markers.

Scale-to-zero (`min_replicas = 0`, `cooldown_period_in_seconds = 300`) lives in `infra/azure.tf:73-75` —
it's what makes the app free to run and what causes the cold start.

## Locked decisions — don't re-litigate

- **Edge SPA via Cloudflare Worker, not Pages.** `run_worker_first = ["/api/*"]` keeps the API on the
  **same origin** → no prod CORS, no `VITE_API_BASE_URL`, `src/api/client.ts` untouched. Pages needs all three.
- **The Docker image keeps serving the SPA from `wwwroot`** — permanently. The Worker is an accelerator
  layered on top, never a dependency; `docker run -e ConnectionStrings__Zmg=… ghcr.io/…` must always
  produce a complete working app, and the ACA URL stays a working rollback target.
- **Plain `chiseled`, not `chiseled-extra`.** Audited: no `TimeZoneInfo`, no `CultureInfo`, all
  comparisons `Ordinal`/`Invariant`, all string ordering is EF → SQL `ORDER BY` (Postgres collation, not
  .NET). v2.8's i18n keeps the server culture-free by construction, so ICU/tzdata buy nothing.
- **Migrations move to the deploy pipeline** via an EF bundle.
- **No added infra cost.** Everything here is $0/mo; priced alternatives are in M41's rejected table.

**Blast radius** (per CLAUDE.md): M39 infra+docs → no tests. M40 API → full `dotnet test`. M41 API +
Dockerfile + pipeline → full `dotnet test` **and** a real deploy. M42 SPA-only → `pnpm lint` + `pnpm build`.

---

## M39 — Terraform state → Azure Storage (encrypted, with locking)

```
[x] 1. Resource group + storage account   zmg-tfstate-rg / zmgtfstate1
[x] 2. Grant yourself data access
[x] 3. Versioning + soft delete
[x] 4. Create the container
[x] 5. Backend block                    ← Code change
[x] 6. Back up local state, migrate
[x] 7. Verify, delete local state       plan: No changes; 9/9 resources
[x] 8. Test locking                     second plan refused on blob lease
[x] 9. infra.yml + README               ← Code change
```

**Cost ~$0/mo:** ~32KB blob, fractions of a cent in storage and transactions; SSE encryption free by
default; **locking uses a native blob lease — no lock table, no charge** (unlike AWS's paid DynamoDB table).

**What changes for the user:** nothing except ~1–3s per command. `terraform.tfvars` stays local (the
backend moves *state*, not inputs). Terraform locks on `plan` too, not just `apply`. `deploy.yml` is
unaffected — CI doesn't run Terraform. An interrupted run leaves the lease held → `terraform force-unlock <ID>`.

**Steps 1–4 — Terminal (you).** Storage account name is globally unique, 3–24 lowercase alphanumeric.

```bash
az group create -n zmg-tfstate-rg -l eastus

az storage account create \
  --name zmgtfstate<suffix> --resource-group zmg-tfstate-rg --location eastus \
  --sku Standard_LRS --kind StorageV2 \
  --min-tls-version TLS1_2 --https-only true \
  --allow-blob-public-access false --allow-shared-key-access false

az role assignment create \
  --role "Storage Blob Data Contributor" \
  --assignee $(az ad signed-in-user show --query id -o tsv) \
  --scope $(az storage account show -n zmgtfstate<suffix> -g zmg-tfstate-rg --query id -o tsv)

az storage account blob-service-properties update \
  --account-name zmgtfstate<suffix> --resource-group zmg-tfstate-rg \
  --enable-versioning true \
  --enable-delete-retention true --delete-retention-days 30 \
  --enable-container-delete-retention true --container-delete-retention-days 30

az storage container create --name tfstate \
  --account-name zmgtfstate<suffix> --auth-mode login
```

Three things here are deliberate, not incidental:
- **`zmg-tfstate-rg` is separate from `zmg-rg`** so a `terraform destroy` can't delete the state file
  describing what it's destroying.
- **`--allow-shared-key-access false`** disables account keys entirely — there's no key to leak. That's
  what makes the role assignment mandatory and `use_azuread_auth` required below.
- **Role assignments take 2–5 min to propagate.** A 403 in step 4 or 6 is almost always propagation, not
  misconfiguration. Wait and retry before changing anything.

**Step 5 — Code change**, `infra/versions.tf`. Backend blocks can't take variables; these are hardcoded
and none are secrets.

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "zmg-tfstate-rg"
    storage_account_name = "zmgtfstate<suffix>"
    container_name       = "tfstate"
    key                  = "zmg.tfstate"
    use_azuread_auth     = true
  }
}
```

**Step 6 — Terminal (you).** Back up `infra/terraform.tfstate` outside the repo first, then
`terraform init -migrate-state` (answer yes to copying state up).

**Step 7 — Terminal (you). This is the safety gate.**

```bash
terraform plan        # MUST say "No changes."
terraform state list  # same inventory as before
```

**If `plan` proposes creating/changing/destroying anything, STOP and diagnose before proceeding** — it
means state didn't migrate cleanly and applying would damage live resources. Nothing is broken at that
point: the local backup is intact and reverting `versions.tf` restores the old setup.

Only after both checks pass, delete local `terraform.tfstate` + `.backup` (gitignored, never committed —
this just removes plaintext secrets from disk). Keep the off-repo backup a few weeks.

**Step 8 — Terminal (you).** Two concurrent `terraform plan`s; the second must report the blob lease.

**Step 9 — Code change.** New `.github/workflows/infra.yml`: `terraform fmt -check -recursive`,
`terraform init -backend=false`, `terraform validate`, gated on `paths: ['infra/**']`. **Separate file
because `ci.yml` `paths-ignore`s `infra/**`** so infra changes never trigger it. `-backend=false` means
no Azure credentials needed. Plus `infra/README.md` — new "Remote state" section: location, why it's
outside `zmg-rg`, that it holds live secrets, recovery via blob versions, `force-unlock`.

**Not doing:** granting the CI deploy identity state access — CI doesn't run Terraform today.

**Verification:** `plan` → No changes; `state list` matches; blob present; lock test behaves. No test run.

**Files:** `infra/versions.tf`, `infra/README.md`, new `.github/workflows/infra.yml`.

---

## M40 — Cold-start baseline (measure before optimizing)

```
[x] 1. [boot] timing logs               ← Code change
[x] 2. Measure post-deploy (image pull guaranteed)
[x] 3. Measure post-idle              → no cached case exists; pulls every start
[x] 4. Read the platform/app split    → app 2.0s (1.8s = DB step); rest is platform
[x] 5. Record baseline in PROGRESS.md   ← Code change
```

**Why this exists:** at zero replicas no container exists, and whether the **image must be downloaded**
depends on node cache — guaranteed after a deploy (new tag), a coin flip afterwards. So it's unknown
whether the 17–25s is dominated by the 216MB download or by app startup. M41 contains work for both
cases; this milestone decides which half to keep.

**Step 1 — Code change**, `src/Zmg.Api/Program.cs`: `[boot]` lines off `Environment.TickCount64` at
post-`builder.Build()`, post-DB-step, and `IHostApplicationLifetime.ApplicationStarted`. Permanent.

**Steps 2–3 — Terminal (you).** Measure immediately after a deploy (guaranteed download), then after
>5 min idle (cooldown = 300s, likely cached). **The gap between the two is the image download cost.**
**Outcome: there is no cached case** — Consumption gives no node affinity, so every cold start re-pulls
(3.2–4.2s for a 91MB image). Results and the resulting scope changes are in `plans/PROGRESS.md`.

```bash
fqdn=$(az containerapp show -n zmg-app -g zmg-rg --query properties.configuration.ingress.fqdn -o tsv)
curl -o /dev/null -s -w 'total: %{time_total}s\n' "https://$fqdn/api/health"
```

**Step 4 — Terminal (you).** Two log streams; the split is the point — a long gap before the first
`[boot]` line means platform/download time, slow `[boot]` lines mean app time.

```bash
az containerapp logs show -n zmg-app -g zmg-rg --tail 100                # app: [boot] lines
az containerapp logs show -n zmg-app -g zmg-rg --type system --tail 100  # platform: create + image pull
```

(In Log Analytics: `ContainerAppConsoleLogs_CL` and `ContainerAppSystemLogs_CL`.)

**Step 5 — Code change.** Baseline table in `plans/PROGRESS.md` — total · platform · app init · first
request, for both cases. M41 re-runs the identical measurement and reports the delta.

**Verification:** full `dotnet test`.

**Files:** `src/Zmg.Api/Program.cs`, `plans/PROGRESS.md`.

---

## M41 — API boot path

```
[x] 1. Design-time DbContext factory     ← Code change  (prerequisite for #2)
[x] 2. Migrations out of startup       → app boot 2.0s → 0.18s; DB step 1.8s → 4ms
[x] 3. Swagger dev-only + lazy S3        ← Code change (CORS policy for dev also moved inside block)
                                       → lazy S3 dropped: the singleton is already lazy
[x] 4. Chiseled base image             → 340MB → 181MB uncompressed; Npgsql verified invariant
[ ] 5. Re-measure vs M40                 (needs a deploy of the chiseled image)
```

Ordered by confidence, each independently revertible. **M40 revised this scope:** items 1–2 are worth
~1.8s (90% of all app boot time), item 3 is cleanup worth ~50ms rather than a perf item, and item 4 is
worth ~1.5s — weaker than planned on size (91MB compressed / 340MB uncompressed, not 216MB) but stronger in payout, since
M40 found the image is re-pulled on **every** cold start. A sixth item, **ReadyToRun, was cut outright**
— see "Not doing". Realistic total ≈ **3.3s off a 17.7–28.1s cold start**; the rest is Azure platform
latency with no knob, which is why M42 is the milestone that actually changes what users feel.

### 1–2. Migrations out of the boot path (~2–4s, highest confidence)

`Program.cs:30-34` calls `db.Database.Migrate()` during startup, which blocks the app from listening
**and** forces a Neon wake (free tier suspends after 5 min; wake is ~1.8s median, ~2.6s p95). Azure's
cold-start guidance is "start listening early". New order: migrate in CI → deploy → container starts →
listens.

**Prerequisite — new `src/Zmg.Infra/Data/ZmgDbContextFactory.cs`.** EF's CLI normally boots the API's
startup code to find the DbContext, which runs M35's `Configuration.Validate()` and demands every `R2:*`
setting. That passes locally via `dotnet user-secrets` but **would throw in CI**, so the bundle can't
build without this. A design-time factory takes precedence and decouples tooling from app config:

```csharp
public class ZmgDbContextFactory : IDesignTimeDbContextFactory<ZmgDbContext>
{
    public ZmgDbContext CreateDbContext(string[] args)
    {
        // Generating migrations/bundles needs the provider, not a live connection; the real connection
        // string is passed to the bundle at run time via --connection.
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Zmg")
                 ?? "Host=localhost;Database=zmg;Username=postgres";
        return new ZmgDbContext(
            new DbContextOptionsBuilder<ZmgDbContext>().UseNpgsql(cs).Options);
    }
}
```

**Code change** — `Program.cs` + `appsettings.json`: gate `Migrate()` on `Database:MigrateOnStartup`,
**defaulting to `true`**. That default is load-bearing: `tests/Zmg.Api.Tests/ZmgApiFactory.cs` relies on
the app's own `Migrate()` for the SQLite schema (see its class comment), and local `dotnet run` relies on
it too — both keep working unchanged. Only prod opts out, via `Database__MigrateOnStartup=false` in
`infra/azure.tf`. Fails open: missing var = old, slower, still-correct behavior.

**Code change** — `.github/workflows/deploy.yml`, a step **before** `az containerapp update`:
- `actions/checkout` **at `ref: ${{ inputs.image_tag }}`** — the tag is the commit SHA, so a
  `workflow_dispatch` rollback builds the bundle matching *that* image, not `main`. Getting this wrong
  applies the wrong schema during a rollback.
- `setup-dotnet`, `dotnet tool install --global dotnet-ef --version 8.*` (EF 8 per CLAUDE.md)
- `dotnet ef migrations bundle --project src/Zmg.Infra --startup-project src/Zmg.Api --self-contained -r linux-x64 -o ./migrate`
- `./migrate --connection "$NEON_CONNECTION_STRING"`

Built in `deploy.yml` rather than passed from `ci.yml` so `workflow_dispatch` rollbacks stay self-contained.

**Terminal (you):** add environment secret `NEON_CONNECTION_STRING` under **Settings → Environments →
production** (same value as the ACA `neon-conn` secret). Environment-scoped, not repo-scoped.

Free safety win: migrations now run before the image swaps, so a failed migration aborts the deploy and
schema/code can't diverge.

### 3. Swagger dev-only + lazy S3 client (a few hundred ms)

- `Program.cs:24-25` registers `AddEndpointsApiExplorer()`/`AddSwaggerGen()` unconditionally but only
  uses them inside `IsDevelopment()` — move the registrations inside that block.
- Revert `R2StorageService` to `Lazy<IAmazonS3>` (M35 made it eager). No safety lost: the fail-fast
  guarantee is `StartupValidationExtensions`, which validates **configuration**, not a built client.
  Update the now-wrong comments at `ServiceRegistrationExtensions.cs:24-30`.

### 4. Chiseled base image (~1.5s — M40 confirmed the image is pulled on every cold start)

`mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled` + `<InvariantGlobalization>true</InvariantGlobalization>`
in `Zmg.Api.csproj` (.NET 8 refuses to start without ICU unless invariant mode is explicit). Safety
audited — see locked decisions.

**Verification gap:** `InvariantGlobalization` lands in the *executable's* runtimeconfig, which the test
host doesn't inherit, so `dotnet test` alone won't catch a regression. Prove it once:

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test
```

**Trade-off accepted:** no shell, so `az containerapp exec` loses bash. Low cost here — the app is
stateless, and env vars/secrets stay readable via `az containerapp show` / `az containerapp secret show`.
Non-root is fine: binds :8080 (>1024), writes nothing to disk.

### Not doing

| Option | Why not |
|---|---|
| In-region Azure Container Registry | ~$5/mo. Would genuinely cut download time — the only rejection that costs real performance |
| `min_replicas = 1` / keep-warm ping | ~$5–15/mo. 24/7 at 0.5 vCPU / 1 GiB ≈ 1.3M vCPU-s vs. a 180,000 vCPU-s free monthly grant |
| Native AOT | Incompatible with EF Core, Swashbuckle, ImageSharp |
| Assembly trimming | EF Core reflection makes it unsafe |
| EF compiled model | Now possible (M36 removed the query filters), but EF's guidance is "hundreds of entity types"; this has 8. M40 measured the whole `built` phase at 135ms, so there is nothing here to win |
| **ReadyToRun** (`-p:PublishReadyToRun=true`) | **Cut after M40.** Precompiling app output would trade +10–15MB for ~0.3–0.8s of JIT — but M40 found only ~200ms of JIT-sensitive window outside the DB step, and that the image is re-pulled on *every* cold start, so the size cost (~0.5s of pull) is paid every time while the saving isn't. Net negative on this workload |

One free option offered but not adopted: a **single** scheduled wake (GH Actions cron, e.g. 13:00 UTC
weekdays) ≈180 vCPU-s per ping against the 180,000 grant. Only helps if usage clusters predictably.

**Step 5 — Terminal (you).** Re-run M40's exact measurement. Confirm the migration step runs
and that a deliberately broken migration aborts the deploy before the image swaps. Confirm `docker run`
still serves the SPA standalone.

**Verification:** full `dotnet test`, plus the invariant-mode run if item 4 landed, plus a real deploy.

**Files:** `src/Zmg.Api/Program.cs`, `appsettings.json`, `Zmg.Api.csproj`, `Services/R2StorageService.cs`,
`Extensions/ServiceRegistrationExtensions.cs`, new `src/Zmg.Infra/Data/ZmgDbContextFactory.cs`,
`Dockerfile`, `.github/workflows/deploy.yml`, `infra/azure.tf`, root `README.md`.

---

## M42 — Edge-served SPA (Cloudflare Worker + same-origin `/api` proxy)

```
[ ] 1. Cloudflare API token (Workers Scripts · Edit)
[ ] 2. wrangler.toml + worker.ts        ← Code change
[ ] 3. build:edge script                ← Code change
[ ] 4. Manual deploy, verify
[ ] 5. Early wake + loading hint        ← Code change
[ ] 6. web.yml + secrets                ← Code change
[ ] 7. Document in infra/README.md      ← Code change
```

**Why this is the milestone that matters:** today the browser can't fetch `index.html` until the
container is up, so the blank page lasts the *entire* cold start. M41 gets 17–25s → maybe 10–16s, and no
free change gets a scale-to-zero container under ~8s. Only this removes the blank screen.

```
Today:  browser ──────────────────────► ACA container      (blank for the full cold start)
M42:    browser ──► Cloudflare edge ──┬─► static files     (~50ms, always warm)
                                      └─► /api/* ──► ACA   (still slow; UI already up)
```

**Cost $0** — static-asset requests are free/unmetered; only proxied `/api/*` counts toward the free
plan's 100k req/day.

**Code change** — `src/Zmg.Web/worker.ts` (only runs for `/api/*`):

```ts
export default {
  async fetch(request: Request, env: { API_ORIGIN: string }): Promise<Response> {
    const url = new URL(request.url);
    return fetch(new Request(new URL(url.pathname + url.search, env.API_ORIGIN), request));
  },
};
```

**Code change** — `src/Zmg.Web/wrangler.toml`:

```toml
name = "zmg-tracker"
main = "worker.ts"
compatibility_date = "2026-07-01"

[assets]
directory          = "./dist"
not_found_handling = "single-page-application"   # deep links (/catalog/<id>) serve index.html
run_worker_first   = ["/api/*"]                  # everything else never touches the Worker

[vars]
API_ORIGIN = "https://zmg-app.<env-hash>.eastus.azurecontainerapps.io"
```

**Code change** — `package.json`: `build:edge` = `tsc -b && vite build --outDir dist`, reusing the same
`--outDir` override the Dockerfile's web stage already applies. Existing `pnpm build` →
`../Zmg.Api/wwwroot` unchanged (the container still needs it).

**Terminal (you) — step 1.** Cloudflare API token with **Workers Scripts · Edit** — a *different* token
from the R2 one in `terraform.tfvars`; don't reuse. Also grab the account ID.

**Terminal (you) — step 4.** Deploy by hand once before automating, so failures are readable:
`pnpm build:edge && pnpm dlx wrangler deploy` from `src/Zmg.Web`. Test against a genuinely cold container
(idle >5 min).

**Code change — step 5.** Fire-and-forget `fetch('/api/health')` in `index.html`'s `<head>`, next to the
pre-paint theme script — starts the container waking while the JS bundle downloads (~1–2s overlap,
same-origin GET so no preflight). Plus a loading hint after ~3s ("server is waking up") instead of an
indefinite spinner. `QueryClient` (`App.tsx:21-23`) keeps its 60s `staleTime`.

**Code change — step 6.** New `.github/workflows/web.yml` on push to `main`: `pnpm build:edge` +
`wrangler deploy`, secrets `CLOUDFLARE_API_TOKEN` + `CLOUDFLARE_ACCOUNT_ID`. **No path filter,
deliberately** — SPA and API always ship together so a DTO change can't half-deploy.

**Code change — step 7.** `infra/README.md`, under the existing "Deliberately not managed by Terraform":
the Worker is hand-created because managing it needs Workers Scripts · Edit added to the Cloudflare token
(currently R2-only). `cloudflare_workers_script` is the option if that changes.

**Verification:** `pnpm lint` + `pnpm build`. Force scale-to-zero, load the Worker URL — UI must paint
immediately with data arriving after, never a blank page. Browser-verify a full CRUD path through the
proxy **including a cover upload** (`multipart/form-data`, most likely thing to break behind a proxy), at
375px and desktop, light + dark. Confirm the ACA URL still serves the app directly.

**Files:** new `src/Zmg.Web/wrangler.toml`, new `src/Zmg.Web/worker.ts`, `package.json`, `index.html`,
a loading-hint component, new `.github/workflows/web.yml`, `infra/README.md`, root `README.md`.

---

## v2.8 — deferred to its own plan (`build-plan-2.8.md`)

Multilingual EN/ES, layered so each layer ships independently:

- **L1 — UI chrome via react-i18next.** `i18next`+`react-i18next`; `src/i18n/` with `en.json`/`es.json`,
  `fallbackLng:'en'`. Language persisted via `usePersistedState('lang', …)` and stamped on `<html lang>`
  pre-paint (mirror the theme inline script). **The language selector deferred from M37 lands here**, in
  the navbar before the theme toggle, wired to `i18n.changeLanguage`. Migrate ~150–250 strings
  feature-by-feature; client-side error *fallbacks* translate here.
- **L2 — DB-authored checklist content.** Template/task text translated in the DB (editable without a
  deploy). Recommended schema: a `TemplateTaskTranslation(TemplateTaskId, Locale, Text)` child table
  (jsonb the lighter alt). Localize standard concrete tasks via a stable `Code` on the template task +
  `SourceCode` on the copy, resolved by locale with English fallback (or ship templates-editor-only
  first). SPA sends the active language (`Accept-Language`/`X-Lang`); seed EN+ES for the 31 single + 40
  album tasks — **Spanish content is the gating input**.
- **L3 — API messages as stable codes.** `Validation`/`ReleaseWarnings`/service `OperationResult`
  strings → culture-invariant codes; UI maps code → i18next key (generalize `serverMessages.ts`),
  English fallback during migration. Touches Domain + services + contracts + tests.

**Keep the server culture-free.** M41 ships plain `chiseled` with `InvariantGlobalization=true`, safe
*because* all three layers translate in the browser or the DB. If a layer ever needs server-side `.resx`
with `CurrentUICulture`, or server-side date/number **formatting**, switch to `chiseled-extra` (+33MB) in
the same change — don't discover it in prod. Spanish **collation** (`ñ` between `n` and `o`) is a Postgres
setting, not a .NET one, and is unaffected by the image.

**Pre-existing divergence to watch:** `SongService.cs:31` searches via
`EF.Functions.Like(s.Title.ToLower(), …)`. Postgres' `lower()` is Unicode-aware; **SQLite's is ASCII-only**,
so accented Spanish titles may match in prod but not in the SQLite-backed tests. Unrelated to the image —
it just starts mattering once there's Spanish content.
