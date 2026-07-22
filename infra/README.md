# infra — Terraform (M32)

One root module spanning three providers, codifying the hosting that M29–M31 stood up by hand.
A single `terraform apply` builds one dependency graph across all three and keeps one unified state,
which is what lets the Neon and Cloudflare values flow straight into the Azure container app's config.

| Provider | Version | Manages |
|---|---|---|
| `hashicorp/azurerm` | `~> 4.81` | resource group, Log Analytics workspace, ACA environment, container app |
| `kislerdm/neon` | `0.13.0` (exact) | the Neon project (Postgres) |
| `cloudflare/cloudflare` | `~> 5.12` | the R2 bucket holding cover images |

`kislerdm/neon` is a community provider — Neon publishes no official one, and their docs point here.
It is pinned **exactly** rather than with `~>`: a `0.x` release makes no compatibility promise between
minor versions.

## Bootstrap

```bash
cp terraform.tfvars.example terraform.tfvars   # then fill in every value
terraform init
terraform plan
```

Azure auth comes from your existing `az login`. Neon and Cloudflare need API keys in `terraform.tfvars`
(Neon: Account settings → API keys; Cloudflare: a custom token with **Account · Workers R2 Storage ·
Edit**).

## This config was imported, not created

Every resource here predates the config — M32 adopted the hand-built stack via `import` blocks
(`imports.tf`) rather than recreating it, so prod never went down and no data moved. The blocks are
idempotent no-ops now and are kept as provenance.

The consequence: **the config must match reality, not the other way around.** If a plan proposes
`forces replacement`, the config is wrong. Two of those would be unrecoverable:

- **`neon_project`** — replacement deletes the production database. `pg_version`, `region_id` and
  `org_id` are immutable; treat any diff on them as a bug in the config.
- **`cloudflare_r2_bucket`** — replacement means every stored cover. `location` is creation-only.

## Deliberately not managed

- **The R2 S3 access key and secret** (`r2_access_key_id`, `r2_secret_access_key`) are made by hand in
  the Cloudflare dashboard and passed in as variables. This is not laziness: a token created by
  `cloudflare_api_token` returns 403 when used as an S3 credential, because R2's access-key derivation
  isn't exposed through the provider — see
  [cloudflare/terraform-provider-cloudflare#6626](https://github.com/cloudflare/terraform-provider-cloudflare/issues/6626).
- **The running image tag.** `azurerm_container_app.zmg` carries
  `lifecycle { ignore_changes = [template[0].container[0].image] }` so deploys can ship a new tag
  without Terraform reverting it. Terraform owns the infrastructure; the delivery pipeline owns the
  application version. `var.container_image` is therefore a **bootstrap default, not the truth** —
  read the live tag from Azure, not from this repo. Automating that pipeline is M34.

## Secrets and state

`terraform.tfstate` is **local and gitignored**, and holds the Neon password, the R2 secret and the
GHCR token **in cleartext** — `sensitive = true` redacts values from CLI output and outputs, but
encrypts nothing at rest. Also gitignored: `terraform.tfvars`, `.terraform/`, `generated.tf`.
`.terraform.lock.hcl` **is** committed, same role as `pnpm-lock.yaml`.

Move to a remote backend (Azure Storage, encrypted, with locking) the moment a second person or CI
needs to apply.

## Wiring

- `local.neon_connection_string` is composed from `neon_project.zmg`'s own attributes — Npgsql wants
  `keyword=value`, and Neon's `connection_uri` is a `postgresql://` URI it can't parse. It backs the
  `neon-conn` secret behind `ConnectionStrings__Zmg`, so rotating the Neon role propagates on the next
  apply instead of being re-copied by hand.
- `R2__Bucket` reads `cloudflare_r2_bucket.covers.name`, so the app can't be pointed at a bucket this
  config doesn't manage. The other four `R2__*` values come from variables.
