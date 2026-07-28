# infra — Terraform

Infrastructure-as-code for the ZMG Release Tracker's hosted stack. One root module spans three
providers and keeps a single state, so one `terraform apply` builds a unified dependency graph and the
database/storage values flow straight into the app's configuration.

| Provider | Version | Manages |
|---|---|---|
| `hashicorp/azurerm` | `~> 4.81` | resource group, Log Analytics workspace, Container Apps environment + app, the GitHub Actions deploy identity |
| `kislerdm/neon` | `0.13.0` (exact) | the Neon project (Postgres) |
| `cloudflare/cloudflare` | `~> 5.12` | the R2 bucket holding cover images |

`kislerdm/neon` is a community provider — Neon publishes no official one. It is pinned **exactly**
rather than with `~>`, because a `0.x` release makes no compatibility promise between minor versions.

## Files

| File | Contents |
|---|---|
| `versions.tf` | `required_providers` + version pins, and the `azurerm` remote state backend |
| `providers.tf` | provider auth (Azure via `az login`; Neon + Cloudflare via API-key vars) |
| `variables.tf` | all inputs |
| `azure.tf` | resource group, Log Analytics, Container Apps environment + its diagnostic setting, the container app + its secrets/env |
| `neon.tf` | the Neon project and the composed connection string (`local.neon_connection_string`) |
| `cloudflare.tf` | the R2 bucket |
| `deploy-identity.tf` | the managed identity, federated credential, and role assignment the CI/CD pipeline uses |
| `outputs.tf` | app URL and the deploy identity's client/tenant ids |
| `imports.tf` | `import` blocks that adopted the hand-built resources into state |
| `terraform.tfvars.example` | template for the gitignored `terraform.tfvars` |

## Bootstrap

```bash
cp terraform.tfvars.example terraform.tfvars   # then fill in every value
terraform init
terraform plan
```

Azure auth comes from your existing `az login`. Neon and Cloudflare need API keys in `terraform.tfvars`
(Neon: Account settings → API keys; Cloudflare: a custom token with **Account · Workers R2 Storage ·
Edit**).

## The config was imported, not created from scratch

The live resources predate this config and were adopted with `import` blocks rather than recreated, so
the config must **match reality**, not the other way around. If a plan proposes `forces replacement`,
the config is wrong — fix the config. Two replacements would be unrecoverable:

- **`neon_project`** — replacement deletes the production database. `pg_version`, `region_id` and
  `org_id` are immutable; any diff on them is a bug in the config.
- **`cloudflare_r2_bucket`** — replacement means every stored cover. `location` is creation-only.

## Deliberately not managed by Terraform

- **The R2 S3 access key and secret** (`r2_access_key_id`, `r2_secret_access_key`) are created by hand
  in the Cloudflare dashboard and passed in as variables. A token created by `cloudflare_api_token`
  returns 403 when used as an S3 credential — R2's access-key derivation isn't exposed through the
  provider ([cloudflare/terraform-provider-cloudflare#6626](https://github.com/cloudflare/terraform-provider-cloudflare/issues/6626)).
- **The running image tag.** `azurerm_container_app.zmg` sets
  `lifecycle { ignore_changes = [template[0].container[0].image] }`, so the CI/CD pipeline can ship a
  new tag without Terraform reverting it. Terraform owns the infrastructure; the pipeline owns the
  application version. `var.container_image` is a **bootstrap default, not the live tag** — read the
  running tag from Azure, not from this repo.
- **The state backend itself** (`zmg-tfstate-rg` + the `zmgtfstate1` storage account) is created by
  hand with `az` — Terraform can't create the account its own state lives in. Setup steps are in
  [plans/build-plan-2.7.md](../plans/build-plan-2.7.md) (M39).
- **The Cloudflare Worker *script*** (`zmg-tracker`) is created by `wrangler`, not Terraform, and
  deploys from [`.github/workflows/web.yml`](../.github/workflows/web.yml) with its own token. The
  pipeline owns application code; Terraform owns infrastructure — same split as the image tag above.
- **The `zionmusicgroup.com` zone and its three DNS records** (apex `A` → Netlify, `www` `CNAME` →
  Netlify, `MX` → Google Workspace) are hand-managed in the Cloudflare dashboard, **deliberately**.
  Codifying them would need `Zone: Write` + `DNS: Write` on the Terraform token, and a Terraform-driven
  replacement of a DNS record is a brief outage while a replaced **MX record is lost mail**. Three
  static records, recorded verbatim in [`plans/build-plan-2.10.md`](../plans/build-plan-2.10.md) (M53),
  do not earn that.
- **The Worker *custom domain*** (`app.zionmusicgroup.com`) **is** Terraform-managed as
  `cloudflare_workers_custom_domain` (v2.10/M53). This is the one place the Cloudflare token in
  `terraform.tfvars` widens beyond R2: the resource accepts **`Workers Scripts Read` + `Write`** and
  nothing more — notably **no DNS rights**, because Cloudflare creates the proxied `app` record and
  issues the certificate itself as a side effect of the binding.
  - The rejected alternative was declaring the domain in `wrangler.jsonc`, which would have required
    **`DNS: Edit`** on the *CI* token — a credential in GitHub Actions able to rewrite the DNS that
    routes company email. Keeping the wider credential in gitignored `terraform.tfvars` and out of CI
    is the whole point of the choice.

## Logging

The environment's `logs_destination` is **`azure-monitor`**, not `log-analytics`. That is the only
setting under which the platform emits `ContainerAppHTTPLogs` — a per-request ingress record (method,
path, status, duration, request id, client IP) written by Envoy at no CPU cost to the app.

**`azurerm_monitor_diagnostic_setting.zmg_env` is the plumbing, not an accessory.** Under this
destination nothing else routes logs: delete that resource and logging stops silently, with the app
running normally and no error anywhere. It carries three categories — console, system, and HTTP.

Three things about the resource that are easy to get wrong:

- **`log_analytics_workspace_id` does not go on the environment** in this mode; the provider rejects
  the combination outright. The workspace is named on the diagnostic setting instead.
- **`log_analytics_destination_type` is deliberately omitted.** Container Apps' categories are
  resource-specific only, so Azure never persists the field and Terraform re-proposes it on every
  plan — a permanently dirty plan is how a real diff hides. Rows land in the resource-specific tables
  regardless.
- **The switch was checked against replacement before it ran.** Replacing
  `azurerm_container_app_environment` would change the ACA FQDN, which is `API_ORIGIN` in
  `wrangler.jsonc` *and* a registered Google redirect URI. The plan reported `~ update in-place`, so it
  proceeded; had it said `forces replacement`, the answer was no.

Console and system logs live in `ContainerAppConsoleLogs` / `ContainerAppSystemLogs`. Anything from
before the switch is in the `_CL` custom tables with `_s`-suffixed columns and was **not** migrated.
Retention is 30 days. Queries: [`docs/kql-cookbook.md`](../docs/kql-cookbook.md).

## Deploy identity (OIDC)

`deploy-identity.tf` lets GitHub Actions deploy without a stored Azure secret:

- a **user-assigned managed identity** with no password of its own;
- a **federated credential** trusting tokens from GitHub's issuer whose subject is
  `repo:danielsantil/zmg-tracker:environment:production` — matched as an **exact string**, so it must
  equal the GitHub Environment name the deploy job runs in (`production`); a mismatch surfaces as
  `AADSTS70021`;
- a **role assignment** granting that identity **Container Apps Contributor** scoped to the app alone.

The GitHub side pairs with it: a repo **Environment** named `production` and three repo **Variables**
(not Secrets — these are identifiers, and masking them only makes OIDC failures harder to debug):
`AZURE_CLIENT_ID` (`terraform output deploy_client_id`), `AZURE_TENANT_ID`
(`terraform output deploy_tenant_id`), and `AZURE_SUBSCRIPTION_ID`.

## Edge SPA (Cloudflare Worker)

The SPA is served from Cloudflare's edge at **https://app.zionmusicgroup.com** (and still on
`zmg-tracker.zmg-app.workers.dev`, kept as a second path), configured by
[`src/Zmg.Web/wrangler.jsonc`](../src/Zmg.Web/wrangler.jsonc) and deployed by
[`.github/workflows/web.yml`](../.github/workflows/web.yml) after each successful ACA deploy.

**Why it exists:** ACA scales to zero, and a cold start is ~17–22s — dominated by Azure sandbox
provisioning that no code change can touch (see `plans/PROGRESS.md`, M40/M41). Previously the browser
couldn't fetch `index.html` until the container was up, so the whole cold start was a blank page. Now
the shell arrives in ~150ms and only the data waits.

- **`run_worker_first: ["/api/*"]`** means the Worker runs *only* for API paths; every static asset is
  served straight from the edge without executing any code. `worker.ts` forwards those requests to
  `API_ORIGIN` (the ACA FQDN, a plain `var` — not a secret).
- **Same origin is the point.** Because `/api/*` lives on the Worker's hostname, there is no prod CORS
  policy, no `VITE_API_BASE_URL`, and `src/api/client.ts` needed no changes. Serving the SPA from a
  separate origin would have required all three.
- **`not_found_handling: "single-page-application"`** makes deep links like `/catalog/<id>` return
  `index.html` instead of 404.
- **The Worker is an accelerator, never a dependency.** The container keeps building and serving the SPA
  from `wwwroot`, so the ACA URL stays a complete, working app and a valid rollback target. Don't remove
  the SPA from the image.
- **`pnpm build`** → `../Zmg.Api/wwwroot` (for the container). **`pnpm build:edge`** → `./dist` (for the
  Worker). Both must keep working.
- Worker types are generated: `pnpm exec wrangler types` writes `worker-configuration.d.ts` (committed),
  which is where the `Env` type for `API_ORIGIN` comes from. Re-run it after changing `vars` or bindings.

## Migrations and rollback

As of M41 the app **does not migrate on startup in prod** — `Database__MigrateOnStartup=false` is set on
the container app, and `deploy.yml` applies migrations instead, via an EF bundle built from source and
run **before** the image is swapped. Two consequences worth knowing: a failed migration aborts the deploy
while the old image is still serving, and the container no longer waits on a Neon wake before it listens.

Locally and in tests the setting is absent, so the default (`true` in `appsettings.json`) applies and
`Program.cs` migrates as before — `dotnet run` still gives a ready database, and the API integration
tests still get their SQLite schema from it.

The bundle is built with `actions/checkout` at **`ref: <image_tag>`**, not at `main`. The tag is a commit
SHA, so a `workflow_dispatch` rollback builds the migrations belonging to *that* image.

**Rolling back the image does not roll back the schema.** EF migrations are forward-only; a bundle built
at an older tag finds all of its own migrations already applied, ignores the newer rows in
`__EFMigrationsHistory` it doesn't recognise, and exits without doing anything. The `ref` pin exists to
stop the opposite failure — checking out `main` during a rollback and applying migrations *newer* than
the image being deployed.

So the database stays ahead, and the older image runs against a newer schema. Whether that survives
depends on the migration:

- **Additive** (new table, new nullable column) — old code ignores it; rollback is clean.
- **Destructive** (dropped or renamed column, a new NOT NULL) — old code breaks.
  `20260723201616_DropSoftDelete` is exactly this: rolling back to a pre-M36 image would query columns
  that no longer exist.

**Rollback is safe to any tag sharing the current schema, and across additive-only migrations. It is not
safe across a destructive one** — that needs a deliberate manual schema revert first. To keep rollback a
real safety net rather than a best-effort one, use expand/contract: ship the additive migration plus code
that tolerates both shapes, deploy, and drop the old column in a *later* migration once you'd never roll
back that far.

## Wiring

- `local.neon_connection_string` is composed from the `neon_project` attributes — Npgsql wants
  `keyword=value` and Neon's `connection_uri` is a `postgresql://` URI it can't parse. It backs the
  `neon-conn` secret behind `ConnectionStrings__Zmg`, so rotating the Neon role propagates on the next
  apply instead of being copied by hand.
- `R2__Bucket` reads `cloudflare_r2_bucket.covers.name`, so the app can't point at a bucket this config
  doesn't manage. The other four `R2__*` values come from variables.

## Remote state

State lives in Azure Storage, not on one laptop. Configured by the `backend "azurerm"` block in
`versions.tf`; those values are hardcoded because backend blocks can't take variables, and none of
them are secret.

| | |
|---|---|
| Resource group | `zmg-tfstate-rg` |
| Storage account | `zmgtfstate1` |
| Container / blob | `tfstate` / `zmg.tfstate` |

- **The resource group is separate from `zmg-rg` on purpose** — so a `terraform destroy` can't delete
  the state file describing what it is destroying.
- **The blob still holds live secrets in cleartext** — Neon password, R2 secret, GHCR token, Google
  OAuth client secret.
  `sensitive = true` redacts values from CLI output; it encrypts nothing. What protects it is access
  control: blob storage is encrypted at rest (SSE, on by default), and **shared key access is disabled
  on the account**, so no account key exists to leak or to be pasted into a CI variable. The only way
  in is an Entra identity holding **Storage Blob Data Contributor** on the account. A new machine or a
  second person needs that role assignment, then `terraform init` — nothing to copy by hand. Role
  assignments take 2–5 minutes to propagate; a `403 AuthorizationPermissionMismatch` right after
  granting is propagation, not misconfiguration.
- **Locking is automatic**, via a native blob lease (no lock table, no cost), and applies to `plan` as
  well as `apply`. An interrupted run leaves the lease held, and every later command then fails with
  `Error acquiring the state lock`. Take the `ID` from that error and run `terraform force-unlock <ID>`
  — only once you're certain no other run is actually live.
- **Recovery:** blob versioning is enabled, with 30-day soft delete for both blobs and containers. A
  corrupted state can be rolled back to an earlier version via the portal (Storage account →
  Containers → `tfstate` → `zmg.tfstate` → Versions) or `az storage blob list --include v`.

Still local and gitignored: `terraform.tfvars` (those are *inputs* — the backend moved state, not
configuration), `.terraform/`, `generated.tf`. `.terraform.lock.hcl` **is** committed, same role as a
lockfile.
