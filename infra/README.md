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
| `azure.tf` | resource group, Log Analytics, Container Apps environment, the container app + its secrets/env |
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
- **The blob still holds live secrets in cleartext** — Neon password, R2 secret, GHCR token.
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
