# ZMG Release Tracker

Per-release checklist tracker for Zion Music Group. Turns the repeatable pre/release/post checklist
into per-release progress tracking across artists, for singles and albums.

**Live:** https://zmg-app.mangohill-c8bd3207.eastus.azurecontainerapps.io
· **Status:** v2.5 complete — feature-complete through v2.4, fully deployed and on CI/CD.

The source of truth for project state is [plans/PROGRESS.md](plans/PROGRESS.md); per-milestone briefs
are in [plans/build-plan-*.md](plans/). Working conventions are in [CLAUDE.md](CLAUDE.md).

## Stack

| Layer | Tech |
|---|---|
| Backend | ASP.NET Core (.NET 8) minimal API + EF Core |
| Domain | pure C# (no I/O) — template-copy, progress, derived status, warnings, validation |
| Frontend | React 19 + Vite + Tailwind SPA (served from the API's `wwwroot` in prod) |
| Database | **Neon Postgres** (prod + dev); **SQLite in-memory** for tests |
| Image storage | **Cloudflare R2** (release covers, normalized to a 1000px WebP on ingest) |
| Hosting | **Azure Container Apps** (Consumption, scale-to-zero) |
| Infra as code | **Terraform** — `azurerm` + `neon` + `cloudflare` ([infra/](infra/README.md)) |
| CI/CD | **GitHub Actions** — test → build+push image → deploy to ACA via OIDC |

### Architecture

Four projects (`Zmg.sln`), layered so the domain has no I/O. A **Release** (UPC, cover, checklist tasks)
and a **Song** (title, main artist, ISRC, feats/collabs) are separate first-class entities linked
through a pure **`Track`** join, so one song can sit on a single *and* an album. A release copies a
seeded **ChecklistTemplate** into concrete tasks at creation; status and warnings are **derived**, never
stored. See [CLAUDE.md](CLAUDE.md) for the full model.

```
src/Zmg.Domain   Entities, enums, and business rules as pure static classes. No I/O, no EF.
src/Zmg.Infra    ZmgDbContext + EF Core migrations (Npgsql/Postgres); seeds both templates.
src/Zmg.Api      Minimal API — one *Endpoints.cs per resource over a matching *Service.
src/Zmg.Web      React + Vite + Tailwind SPA, feature-sliced under src/features/.
tests/Zmg.Domain.Tests   xUnit unit tests (the layer to unit-test).
tests/Zmg.Api.Tests      Integration tests (WebApplicationFactory + SQLite in-memory).
infra                    Terraform for the whole hosted stack (see infra/README.md).
```

## Prerequisites

- .NET SDK 8.0 (pinned via `global.json`)
- Node.js 24.18.0 (`.nvmrc`) + pnpm (via Corepack — pinned in `package.json`)
- A Postgres connection string in `ConnectionStrings__Zmg` (a Neon dev branch, or local Postgres)

Set the dev connection string once, in user-secrets — it is never committed:

```bash
dotnet user-secrets --project src/Zmg.Api set ConnectionStrings:Zmg "<your-postgres-connection-string>"
```

Cover uploads additionally need the five `R2:*` secrets; without them the app still boots and only
uploading fails. See [CLAUDE.md](CLAUDE.md) for the list.

## Run (development)

Two terminals. The API applies migrations and seeds templates on startup.

```bash
# 1) API on http://localhost:5274
dotnet run --project src/Zmg.Api

# 2) SPA on http://localhost:5173 (dev proxy sends /api to :5274)
cd src/Zmg.Web && pnpm install && pnpm dev
```

Open http://localhost:5173. To change the API port, update `server.proxy` in
`src/Zmg.Web/vite.config.ts` to match.

## Run (production-style, one process)

Build the SPA into the API's `wwwroot`, then run the API — it serves the app and the API together.

```bash
cd src/Zmg.Web && pnpm build      # outputs to ../Zmg.Api/wwwroot
cd ../.. && dotnet run --project src/Zmg.Api
```

Open http://localhost:5274.

## Test / lint

```bash
dotnet test                                            # backend: domain unit + API integration
dotnet test tests/Zmg.Domain.Tests                     # one project
dotnet test --filter "FullyQualifiedName~TemplateCopy" # one class/method

cd src/Zmg.Web
pnpm test          # Vitest (pure modules)
pnpm lint          # eslint
pnpm build         # tsc -b && vite build
```

**Scope verification to the blast radius** — SPA-only changes need `pnpm lint`/`pnpm build`;
domain-only needs `dotnet test tests/Zmg.Domain.Tests`; anything touching a DTO, endpoint, or migration
needs full `dotnet test`. See [CLAUDE.md](CLAUDE.md) for the rules.

## Common tasks

```bash
# EF migrations (keep tooling on EF 8 to match the runtime)
dotnet ef migrations add <Name> --project src/Zmg.Infra --startup-project src/Zmg.Api

# Reset local data: reset the Neon branch, or drop + recreate
dotnet ef database drop --project src/Zmg.Infra --startup-project src/Zmg.Api
dotnet ef database update --project src/Zmg.Infra --startup-project src/Zmg.Api
```

## Deployment

Pushing to `main` runs [`.github/workflows/ci.yml`](.github/workflows/ci.yml): it tests and lints, then
(on green) builds a Docker image tagged with the commit SHA, pushes it to GHCR, and calls
[`deploy.yml`](.github/workflows/deploy.yml), which rolls Azure Container Apps to that tag over **OIDC**
(no stored Azure secret) and smoke-tests `/api/health`.

- **Rollback / redeploy any build:** Actions tab → **Deploy** → **Run workflow** → enter a prior commit
  SHA. It re-points ACA at that existing image — no rebuild.
- **Infrastructure changes** go through Terraform in [infra/](infra/README.md), never the pipeline. The
  pipeline owns the image tag; Terraform owns everything else and ignores the tag by design.
