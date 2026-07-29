# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

ZMG Release Tracker — a per-release checklist tracker for Zion Music Group. It turns a repeatable
pre/release/post checklist into per-release progress tracking across artists, for both singles and
albums. .NET 8 backend (minimal API + EF Core/Postgres via Npgsql; SQLite in-memory for tests) with a
React + Vite + Tailwind SPA.

## Read first

- **`plans/PROGRESS.md`** — the running journal: current state, what shipped per version, and
  what's next. This is the source of truth for project state (the root `README.md` lags behind it).
- **`plans/build-plan-*.md`** — per-milestone scope, rationale, wireframes, and test lists. Newer
  versions are new files; older ones stay frozen. Read latest plan only. No need to read old plans, unless a reference to an old milestone is made.

## Build workflow (follow every time)

When asked to build/implement something without a specific milestone named:

1. **Start by reading `plans/PROGRESS.md`** — its "Backlog / next steps" section names the next
   milestone. Confirm the target with me before writing code if it's ambiguous.
2. Do the work against the relevant `plans/build-plan-*.md` (scope, wireframes, per-milestone test list).
3. Verify **only what the change can break** — do not run the full suite by reflex:
   - **SPA-only** (`src/Zmg.Web`, no API/DTO change): `pnpm lint` and `pnpm build`. Do **not**
     run `dotnet test` or start the API.
   - **Domain-only** (`src/Zmg.Domain`): `dotnet test tests/Zmg.Domain.Tests`, or a `--filter` on the
     class you touched. Skip the API integration tests.
   - **API/Infra**, or anything that changes a DTO, endpoint shape, or migration: full `dotnet test`.
     This is the only case that needs both sides checked.
   - Docs/plans/comments only: no verification.
   When in doubt about the blast radius, ask me rather than running everything.
4. **Finish by updating `plans/PROGRESS.md`** — add a journal entry for what shipped and adjust the
   "Backlog / next steps" so the next session knows the new current state.

## Commands

```bash
# Backend tests (domain unit + API integration)
dotnet test
dotnet test tests/Zmg.Domain.Tests                          # one project
dotnet test --filter "FullyQualifiedName~TemplateCopy"      # one class/method by name

# Run API (http://localhost:5274 — applies migrations + seeds templates on startup)
dotnet run --project src/Zmg.Api

# Run SPA (http://localhost:5173 — dev proxy sends /api to :5274)
cd src/Zmg.Web && pnpm install && pnpm dev

# Lint / typecheck / build the SPA
cd src/Zmg.Web && pnpm lint           # eslint (flat config: eslint.config.js)
cd src/Zmg.Web && pnpm build          # tsc -b && vite build → outputs to ../Zmg.Api/wwwroot

# Production-style single process: build SPA into wwwroot, then run API on :5274
cd src/Zmg.Web && pnpm build && cd ../.. && dotnet run --project src/Zmg.Api
```

- .NET SDK is pinned to 8.0.x via `global.json`. Node 24.18.0
- **Cover images (v2.5/M31): Cloudflare R2.** Uploads go through `/api/uploads/cover*`; the five `R2:*`
  settings live in **dev** `dotnet user-secrets` and (pending) as **prod** ACA secrets. Without them the
  app still boots — only uploading fails. Tests never touch R2 or the network (fake storage + stub handler).
- **Database (v2.5): Postgres (Neon).** Dev + prod use `ConnectionStrings__Zmg` — **dev** via
  `dotnet user-secrets` in `src/Zmg.Api` (never commit it), **prod** as an ACA secret. Startup applies
  migrations + seeds templates. To reset local data: reset the Neon
  branch, or `dotnet ef database drop` + `dotnet ef database update`. **Tests run SQLite in-memory**. EF migrations are Postgres-specific; keep EF tooling on
  **EF 8** to match the runtime.
- **Never apply migrations by hand in dev — `dotnet ef migrations add`, then restart the API.** Dev
  applies migrations on startup, so running `dotnet ef database update` is at best redundant. It also
  does not work: the EF design-time host is not the entry assembly, so it never reads
  `dotnet user-secrets`, finds no connection string, and fails trying to reach `127.0.0.1:5432`. Don't
  work around that by passing `--connection` — that puts the production-grade credential on the command
  line and in shell history. Prod is not migrated this way either; the deploy pipeline does it.
- **Auth (v2.10): Google SSO + an `AllowedUser` whitelist.** Two `Authentication:Google:*` settings in
  **dev** user-secrets and **prod** ACA config; startup validation refuses to boot without them. Local
  sign-in needs `http://localhost:5274/api/auth/google/callback` registered as a redirect URI — the
  *API's* port, not Vite's. **Tests never exercise the Google leg** (it would fetch the discovery
  document over the network); `ZmgApiFactory` stubs a signed-in user by default, and
  `Authenticated = false` gets you the real deny-by-default pipeline.
- **Logs (v2.10): structured JSON to stdout outside Development, no package or sink.** Queries live in
  [`docs/kql-cookbook.md`](docs/kql-cookbook.md).

## Architecture

Four projects (`Zmg.sln`), layered so the domain has no I/O:

- **`src/Zmg.Domain`** — pure C#, no I/O, no EF. Entities (`Entities/`), enums (`Enums/`), and the
  business logic as standalone static classes: `TemplateCopy` (stamp a template's tasks onto a new
  release), `Progress`, `ReleaseStatus` (derived status), `ReleaseWarnings` (soft warnings array),
  `PendingActions` (the "needs attention" engine), `SongArchival`, `Validation`, `SeedData`. **This is
  the layer to unit-test; keep it free of framework/DB dependencies.**
- **`src/Zmg.Infra`** — `ZmgDbContext` + EF migrations. (Note: the DbContext lives here, not in the API.)
- **`src/Zmg.Api`** — ASP.NET Core minimal API. One `*Endpoints.cs` per resource (`MapXEndpoints()`,
  all wired in `Program.cs`) delegating to a matching `I*Service`/`*Service` in `Services/`
  (registered scoped in `Program.cs`). Services return `OperationResult` for success/validation
  outcomes; `Contracts/` holds request/response DTOs. Serves the built SPA from `wwwroot` with an
  SPA fallback in production.
- **`src/Zmg.Web`** — React 19 + Vite + Tailwind, feature-sliced under `src/features/` (`home`,
  `releases`, `catalog`, `templates`, `artists`). `src/api/` is the typed API layer — one module per
  resource over `client.ts`, which throws `ApiError` (carrying the server's `errors[]`) on 4xx/409.

### Core domain model

A **Release** (UPC, cover, checklist tasks) and a **Song** (title, main artist, ISRC, feats/collabs
via `SongArtist`) are separate first-class entities linked through a pure **`Track` join** (composite
PK `(ReleaseId, SongId)`; endpoints under `/api/releases/{releaseId}/tracks/{songId}`). A release copies
a **ChecklistTemplate**'s `TemplateTask`s into concrete `ReleaseTask`s at creation (`TemplateCopy`);
two templates are seeded (single vs. album). Release status and warnings are **derived**, not stored —
compute them via the domain classes rather than persisting them.

## Conventions

- Adding an API resource means the full slice: entity (Domain) → migration (Infra) → `I*Service`+`*Service`
  (registered in `Program.cs`) → `*Endpoints.cs` (mapped in `Program.cs`) → `src/api/*.ts` client module.
  **A new endpoint is authenticated by default** — a fallback authorization policy covers everything, so
  there is no `.RequireAuthorization()` to forget. `.AllowAnonymous()` is the deliberate opt-out, and
  only `/api/health`, `/api/auth/*` and the SPA shell take it.
- Put business rules in `Zmg.Domain` static classes (unit-tested), not in endpoints or services.
- Soft warnings (e.g. missing UPC/ISRC, empty album) are non-blocking — surfaced via
  `ReleaseWarnings`, never enforced as validation errors.
- **The API ships codes, never user-facing prose (v2.8/M46).** Every error/warning is a
  `Zmg.Domain.Message(Code, Args?)` whose `Code` is an i18next key path (`error.song.duplicateTitle`),
  rendered by the SPA. A new code needs a key in **both** `en.json` and `es.json` — `MessageCodeApiTests`
  reflects over the constants and fails otherwise. Codes are permanent identifiers; keep them next to
  the rule that raises them (`Validation`, `ReleaseWarnings`, `CoverImage`), or in
  `Api/Services/ServiceErrors.cs` for the ones a service mints. Full rules in PROGRESS.md.
- **Checklist task text is two columns; a task's identity is its `Code` (v2.9/M49–M52).** `TemplateTask`
  and `ReleaseTask` each carry `TitleEn` + `TitleEs` (nullable — null means "shows the English", a valid
  state). `TemplateCopy` copies both down, so a release owns its checklist in every language and a
  template edit can never reach it. The API **never resolves a locale**: it ships both columns and the
  SPA picks one via `lib/taskText.ts`. **Never key a rule off a task title** — `Release.IsDistributed`
  uses `SourceCode`, and anything new must too; text edits leave the code alone, which is what lets ZMG
  reword a task without silencing the rules. These two columns are the **only** dual-language fields in
  the app; song titles, artist names and notes stay single-value. Full rules in PROGRESS.md.
- **The SPA is EN/ES (v2.8).** No user-facing literal in `src/features/**` or `src/components/**` —
  everything goes through `t()`, keyed in `src/i18n/locales/{en,es}.json` (add to **both**; the parity
  test in `src/i18n/i18n.test.ts` fails otherwise). `t()` is typed off `en.json`, so a wrong key won't
  compile. Never build a sentence by concatenation — use interpolation or `_one`/`_other` plurals; pure
  helpers return shapes and `hooks/useFormatters` supplies the words. Full rules in PROGRESS.md's
  Cross-cutting decisions.
- **Log through `Api/Logging/Log.cs`, never a bare `logger.LogInformation` at a call site (v2.10/M57).**
  Events are `[LoggerMessage]` methods with permanent numeric ids (1000 auth, 2000 uploads, 3000
  requests) so queries filter on `EventId` instead of message text. The happy path stays silent —
  ingress already records every request — so add an event only for something ingress cannot know.
  **Never log** session/cookie material, tokens, the connection string, R2 keys, or query strings; log
  `Request.Path` without them. The single exception is the email on auth events. Full rules in
  PROGRESS.md.
- **Middleware order in `Program.cs` is load-bearing, and two placements are not stylistic.**
  `UseDefaultFiles`/`UseStaticFiles` must stay **before** `UseAuthentication` — an asset path matches no
  endpoint, and "no endpoint" is exactly when the fallback authorization policy applies, so serving them
  later answers an anonymous browser with a redirect and the SPA never boots (`StaticFileAuthApiTests`
  pins it). `UseZmgForwardedHeaders` must stay before authentication, or the OIDC `redirect_uri` is
  built from the wrong host.
