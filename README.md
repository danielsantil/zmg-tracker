# ZMG Release Tracker

Per-release checklist tracker for Zion Music Group. Turns the repeatable
pre/release/post checklist into a per-release progress tracker across artists,
for both singles and albums. See [plans/PROGRESS.md](plans/PROGRESS.md) for current
state and the [plans/build-plan-*.md](plans/) files for per-milestone briefs.

**Status:** v2.4 complete (M26–M28).
See [plans/PROGRESS.md](plans/PROGRESS.md) for the running journal.

## Stack

- **Backend:** ASP.NET Core (.NET 8) minimal API + EF Core (SQLite)
- **Domain:** pure C# (no I/O) — template copy, progress, status, validation
- **Frontend:** React + Vite + Tailwind SPA (served from the API's `wwwroot` in prod)

## Layout

```
src/Zmg.Domain   Entities, enums, template-copy / progress / status / warnings / validation. No I/O.
src/Zmg.Infra    ZmgDbContext + EF Core migrations (SQLite); seeds both templates.
src/Zmg.Api      Minimal API — endpoints + services over the domain and DbContext.
src/Zmg.Web      React + Vite + Tailwind SPA.
tests/Zmg.Domain.Tests   xUnit unit tests (copy, progress, status, validation, seed).
tests/Zmg.Api.Tests      Integration tests (WebApplicationFactory + SQLite in-memory).
```

## Prerequisites

- .NET SDK 8.0 (pinned via `global.json`)
- Node.js 24.18.0

## Run (development)

Two terminals. The API applies migrations and seeds templates on startup.

```bash
# 1) API on http://localhost:5274 (default launch profile)
dotnet run --project src/Zmg.Api

# 2) SPA on http://localhost:5173 (proxies /api to the API on :5274)
cd src/Zmg.Web && pnpm install && pnpm dev
```

Open http://localhost:5173. The SPA's dev proxy targets `:5274`, so run the API on
that port (its default profile). To use a different port, update the `server.proxy`
target in `src/Zmg.Web/vite.config.ts` to match.

## Run (production-style, one process)

Build the SPA into the API's `wwwroot`, then run the API — it serves the app and API together.

```bash
cd src/Zmg.Web && pnpm install && pnpm build   # outputs to ../Zmg.Api/wwwroot
cd ../.. && dotnet run --project src/Zmg.Api
```

Open http://localhost:5274.

## Run (Docker)

One image builds the SPA, publishes the API, and serves both. The SQLite file lives in a
mounted volume so data survives container restarts.

```bash
docker build -t zmg-tracker .
docker run -p 8080:8080 -v zmg-data:/data zmg-tracker
```

Detached:
```bash
docker run -d -p 8080:8080 -v zmg-data:/data --name zmg-tracker zmg-tracker
docker logs -f zmg-tracker     # follow logs
docker stop zmg-tracker        # stop it
docker rm zmg-tracker          # remove (needed before reusing the name)
```

Open http://localhost:8080.

## Test

```bash
dotnet test          # backend: domain unit + API integration
pnpm test            # UI: UI tests
```

## Notes

- The runtime SQLite file (`*.db`) is git-ignored; seed data lives in the migration.
- Schema stays auth-ready and metadata-ready (UPC/ISRC) for later phases; no auth in v1.
