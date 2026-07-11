# ZMG Release Tracker

Per-release checklist tracker for Zion Music Group. Turns the repeatable
pre/release/post checklist into a per-release progress tracker across artists,
for both singles and albums. See [build-plan.md](build-plan.md) for the full brief.

**Status:** Milestones M0 + M1 complete (skeleton, artists + releases CRUD, seeded
checklist templates, release-detail checklist screens and template management are M2+).

## Stack

- **Backend:** ASP.NET Core (.NET 8) minimal API + EF Core (SQLite)
- **Domain:** pure C# (no I/O) — template copy, progress, status, validation
- **Frontend:** React + Vite + Tailwind SPA (served from the API's `wwwroot` in prod)

## Layout

```
src/Zmg.Domain   Entities, enums, template-copy / progress / status / validation. No I/O.
src/Zmg.Api      Minimal API, EF Core + SQLite, migrations, seeds both templates.
src/Zmg.Web      React + Vite + Tailwind SPA.
tests/Zmg.Domain.Tests   xUnit unit tests (copy, progress, status, validation, seed).
tests/Zmg.Api.Tests      Integration tests (WebApplicationFactory + SQLite in-memory).
```

## Prerequisites

- .NET SDK 8.0 (pinned via `global.json`)
- Node.js 20.19+ / 22.12+

## Run (development)

Two terminals. The API applies migrations and seeds templates on startup.

```bash
# 1) API on http://localhost:5218
dotnet run --project src/Zmg.Api --no-launch-profile --urls http://localhost:5218

# 2) SPA on http://localhost:5173 (proxies /api to the API)
cd src/Zmg.Web && npm install && npm run dev
```

Open http://localhost:5173.

## Run (production-style, one process)

Build the SPA into the API's `wwwroot`, then run the API — it serves the app and API together.

```bash
cd src/Zmg.Web && npm install && npm run build   # outputs to ../Zmg.Api/wwwroot
cd ../.. && dotnet run --project src/Zmg.Api --urls http://localhost:5218
```

Open http://localhost:5218.

## Test

```bash
dotnet test          # backend: domain unit + API integration
```

## Notes

- The runtime SQLite file (`*.db`) is git-ignored; seed data lives in the migration.
- Schema stays auth-ready and metadata-ready (UPC/ISRC) for later phases; no auth in v1.
