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
| `versions.tf` | `required_providers` + version pins |
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

## Wiring

- `local.neon_connection_string` is composed from the `neon_project` attributes — Npgsql wants
  `keyword=value` and Neon's `connection_uri` is a `postgresql://` URI it can't parse. It backs the
  `neon-conn` secret behind `ConnectionStrings__Zmg`, so rotating the Neon role propagates on the next
  apply instead of being copied by hand.
- `R2__Bucket` reads `cloudflare_r2_bucket.covers.name`, so the app can't point at a bucket this config
  doesn't manage. The other four `R2__*` values come from variables.

## Secrets and state

`terraform.tfstate` is **local and gitignored**, and holds the Neon password, R2 secret and GHCR token
**in cleartext** — `sensitive = true` redacts values from CLI output, but encrypts nothing at rest.
Also gitignored: `terraform.tfvars`, `.terraform/`, `generated.tf`. `.terraform.lock.hcl` **is**
committed, same role as a lockfile.

Move to a remote encrypted backend (e.g. Azure Storage with locking) before a second person or CI needs
to apply.
