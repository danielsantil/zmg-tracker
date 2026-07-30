# ZMG Release Tracker

Per-release checklist tracker for Zion Music Group. Turns the repeatable pre/release/post checklist
into per-release progress tracking across artists, for singles and albums.

**Live:** https://app.zionmusicgroup.com

The source of truth for project state — what shipped, what's next, and the decisions behind both — is
[plans/PROGRESS.md](plans/PROGRESS.md). Working conventions are in [CLAUDE.md](CLAUDE.md); the plans
folder holds the per-milestone briefs behind each of them.

## Stack

| Layer | Tech |
|---|---|
| Backend | ASP.NET Core (.NET 8) minimal API + EF Core |
| Domain | pure C# (no I/O) — template-copy, progress, derived status, warnings, validation |
| Frontend | React 19 + Vite + Tailwind SPA (served from a **Cloudflare Worker** at the edge, and from the API's `wwwroot` as a fallback) |
| Languages | **English + Spanish** — react-i18next bundles for UI text, stable codes for API messages, DB rows for checklist text |
| Auth | **Google SSO** (OIDC, server-side) + a database whitelist; sessions are revocable Postgres rows |
| Database | **Neon Postgres** (prod + dev); **SQLite in-memory** for tests |
| Image storage | **Cloudflare R2** (release covers, normalized to a 1000px WebP on ingest) |
| Hosting | **Azure Container Apps** (Consumption, scale-to-zero) |
| Edge | **Cloudflare Worker** — serves the SPA, proxies `/api/*` to ACA on the same origin |
| Logs | **Structured JSON to stdout** → Azure Log Analytics, joined to per-request ingress logs ([cookbook](docs/kql-cookbook.md)) |
| Infra as code | **Terraform** — `azurerm` + `neon` + `cloudflare` ([infra/](infra/README.md)) |
| CI/CD | **GitHub Actions** — test → build+push image → deploy to ACA via OIDC → deploy SPA to Cloudflare |

### Architecture

Four projects (`Zmg.sln`), layered so the domain has no I/O. A **Release** (UPC, cover, checklist tasks)
and a **Song** (title, main artist, ISRC, feats/collabs) are separate first-class entities linked
through a pure **`Track`** join, so one song can sit on a single *and* an album. A release copies a
seeded **ChecklistTemplate** into concrete tasks at creation; status and warnings are **derived**, never
stored. See [CLAUDE.md](CLAUDE.md) for the full model.

The app is **bilingual (EN/ES)** in three layers, each with its own mechanism: UI text is i18next JSON
bundled into the SPA; API errors and warnings ship as culture-invariant **codes** the SPA renders (the
server stays culture-free, `InvariantGlobalization=true`); and checklist task text is **data** — a
stable `Code` per seeded task resolving a per-locale row at read time, editable in the templates screen
without a deploy.

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
All of the following must be set, or the API refuses to boot — startup lists every missing key at once:

| Env var | Purpose |
| --- | --- |
| `ConnectionStrings__Zmg` | Postgres connection string (a Neon dev branch, or local Postgres) |
| `R2__AccountId` | Cloudflare R2 account id |
| `R2__AccessKeyId` | R2 access key id |
| `R2__SecretAccessKey` | R2 secret access key |
| `R2__Bucket` | R2 bucket name |
| `R2__PublicBaseUrl` | Public read origin for the bucket (prod: `https://img.zionmusicgroup.com`; dev: the bucket's own r2.dev URL) |
| `Authentication__Google__ClientId` | Google OAuth client id — public by design (it appears in the authorization URL) |
| `Authentication__Google__ClientSecret` | Google OAuth client secret |

Set them once in user-secrets for dev — never committed (`__` in env vars maps to `:` in user-secrets):

```bash
dotnet user-secrets --project src/Zmg.Api set ConnectionStrings:Zmg "<your-postgres-connection-string>"
dotnet user-secrets --project src/Zmg.Api set R2:AccountId "<account-id>"
dotnet user-secrets --project src/Zmg.Api set R2:AccessKeyId "<access-key-id>"
dotnet user-secrets --project src/Zmg.Api set R2:SecretAccessKey "<secret-access-key>"
dotnet user-secrets --project src/Zmg.Api set R2:Bucket "<bucket>"
dotnet user-secrets --project src/Zmg.Api set R2:PublicBaseUrl "<https://…r2.dev>"
dotnet user-secrets --project src/Zmg.Api set Authentication:Google:ClientId "<…apps.googleusercontent.com>"
dotnet user-secrets --project src/Zmg.Api set Authentication:Google:ClientSecret "<client-secret>"
```

> **R2 buckets are per-environment.** Dev writes to its own `zmg-covers-dev` bucket (its own r2.dev
> URL, and a dev-scoped R2 API token); prod uses `zmg-covers` served behind the
> `https://img.zionmusicgroup.com` custom domain. Only the prod bucket and its ACA wiring live in
> Terraform — the **dev bucket** and prod's **custom domain** are hand-managed in the Cloudflare
> dashboard, because the R2 API token deliberately holds no DNS rights (a custom domain writes a DNS
> record).

## Who can sign in

Authentication and authorization are separate, and the split is the whole design.

**Any Google account can authenticate.** The OAuth client is *External* and published, so there is no
second allow-list in the Google console to keep in step. **`AllowedUser` alone decides who gets in** —
anyone else lands on a "not authorised" screen and an `auth.login.denied` log line.

Adding a partner is one row. There is no signup, no invite email and no admin screen, by decision:

```sql
INSERT INTO "AllowedUsers" ("Id", "Email", "DisplayName", "CreatedAt")
VALUES (gen_random_uuid(), 'partner@example.com', NULL, now());
```

The address **must be lowercase and trimmed** — it is stored normalized (`EmailNormalization`) and
compared ordinally, so `Partner@Example.com` simply never matches. `DisplayName` fills itself from
Google's profile on first sign-in.

Revoking has two forms, and they are different tools:

```sql
-- Reversible: denies access on the next request, keeps the record that they were here.
UPDATE "AllowedUsers" SET "DisabledAt" = now() WHERE "Email" = 'partner@example.com';

-- End one session without revoking the person (e.g. a lost laptop).
DELETE FROM "AuthSessions" WHERE "Email" = 'partner@example.com';
```

Sessions are **absolute, not sliding** — 7 days from sign-in via `Auth:SessionDays`, never extended by
use. They live in `AuthSessions` rather than inside the cookie precisely so they can be deleted. The
row is re-checked on every request, so disabling someone takes effect immediately rather than whenever
their week happens to run out.

Authorization is **flat**: on the list means full access. There are no roles and no per-screen rules,
so the SPA has one gate around the whole app rather than per-route guards. On the server the reverse
applies — **every endpoint requires a session unless it explicitly opts out**, so forgetting to protect
something new is a non-event rather than a hole.

**Local dev needs a Google OAuth client** with `http://localhost:5274/api/auth/google/callback` as an
authorized redirect URI — that is the *API's* port, not Vite's, because Google redirects to the
server's callback. `Auth:PostLoginOrigin` then sends the browser back to `:5173`.

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

## Adding translated text

The app holds **three separate bodies of text**, each with its own mechanism. Pick the layer first —
using the wrong one is the common mistake. Full rules in [plans/PROGRESS.md](plans/PROGRESS.md)
→ Cross-cutting decisions.

| The text is… | Layer | Lives in |
|---|---|---|
| UI chrome — labels, buttons, headings, `aria-label`, placeholders, confirm copy | i18next JSON | `src/Zmg.Web/src/i18n/locales/{en,es}.json` |
| An API error or warning the user sees | a stable **code** the SPA renders | code constant in C# + a key in both locale files |
| A seeded checklist task title | **data** — a per-locale DB row | `SeedData.cs` + a migration |

### 1. UI chrome — a new i18next key

1. Add the key to **both** `en.json` and `es.json`, nested by feature (`releases.detail.someLabel`).
   The parity test in `src/i18n/i18n.test.ts` fails if either is missing, blank, or has mismatched
   `{{placeholders}}`.
2. Use it as `t('releases.detail.someLabel')`. `t()` is typed off `en.json`, so a typo won't compile.
3. **Never build a sentence by concatenation** — use interpolation, or `_one`/`_other` plural keys.
   Pure helpers return *shapes* (`{ days: 3 }`); `hooks/useFormatters` supplies the words.

```bash
cd src/Zmg.Web && pnpm test && pnpm build
```

### 2. API errors and warnings — a new code

The API ships **no user-facing prose**. Add a code, then the text the SPA renders for it.

1. Add the constant next to the rule that raises it — `Validation`, `ReleaseWarnings`, `CoverImage`,
   `ReleaseMutability` — or to `Api/Services/ServiceErrors.cs` if a *service* raises it. Name it
   `error.<area>.<rule>` or `warning.<name>`; that string **is** the i18next key path.
2. Raise it: `result.Error(MyCodes.Something)`, or `Message.With(code, ("name", value))` when it
   interpolates.
3. Add the matching key under `error.` / `warning.` in **both** locale files.
4. Codes are **permanent identifiers** — renaming one is a breaking change on both sides at once.

`MessageCodeApiTests` reflects over every code constant and fails if either locale lacks its key.

```bash
dotnet test && cd src/Zmg.Web && pnpm test
```

### 3. Checklist task text — a new seeded task or a translation fix

Both languages are plain columns on the task row — `TitleEn` and `TitleEs` on `TemplateTask` and
`ReleaseTask`. A null `TitleEs` shows the English, which is a valid state rather than a missing
translation. The API ships both columns and never resolves a locale; the SPA picks one in
`lib/taskText.ts`, which is why switching language costs no request.

**To fix existing copy — no code, no deploy:** open **Templates**, use a task's kebab → **Edit**, and
type both languages in the dialog. It edits the template you're looking at (Single or Album), not both.

**To add a new seeded task:**

1. Add a slug to `TaskCodes.cs`, then the seed in `SeedData.BaseTasks` / `AlbumExtraTasks` with that
   code and **both** titles — they sit on the same line, so a missing translation shows up in the diff.
   `SeedDataTests` fails if any seeded task lacks either one.
2. Generate a migration (`HasData` picks up both columns).
3. **Never key a rule off a task title** — use the code, as `Release.IsDistributed` does. Titles are
   display copy in two languages that the user can reword at will; the code is the identity.

**The copy of record is `SeedData.cs` itself** — both languages sit on the same line, so a missing
translation shows up in the diff. Corrections to *existing* text go through the **templates screen**, not
a migration — a release owns its own checklist, so editing a template only shapes future releases.

Domain jargon stays English by rule: DSP/BMI/MLC/SoundExchange/Musixmatch, "smart link", "pre-save",
"waterfall", "multitracks", "splits", "focus tracks".

```bash
dotnet ef migrations add <Name> --project src/Zmg.Infra --startup-project src/Zmg.Api
dotnet test
```

### Adding a whole new language

Add `src/i18n/locales/<code>.json` and a name in `src/i18n/language.ts` for the UI chrome. Checklist
text needs a **third column** (`TitleFr`), one line in `lib/taskText.ts` and one field in
`TaskEditorModal`. This is a deliberate trade for two languages being the real requirement, and the reason
an approach using normalized translation tables was dropped - it is easier to maintain and query translation changes. No other component code changes; the two-language
toggle becomes a popover (which must portal to `<body>`, per the standing popover rule).

## Logs and diagnosing production

Outside Development the app writes **one JSON object per line to stdout** — no logging package, no
sink, no network call, so nothing in the logging path can fail and take the app down with it. Azure
collects it, alongside a per-request record written by the ingress proxy at no cost to the app.

**Start here: [docs/kql-cookbook.md](docs/kql-cookbook.md)** — paste-and-run queries, each with a
"use this when".

Three things worth knowing before you need them:

- **Every request carries an id**, returned on the `x-request-id` response header and stamped on every
  log line the request produces. When something breaks, that id is the whole investigation: it finds
  the ingress record and the app's own lines together. A 500 puts it in the error message on screen, so
  a user can quote it.
- **The happy path is silent, deliberately.** Ingress already records every request, so the app logs
  only failures, requests over `Logging:SlowRequestMs` (1s), and a short list of events worth naming —
  sign-ins, denials, uploads, blocked remote fetches. No lines for a successful request is the system
  working, not a gap.
- **What is never logged, as a rule:** session or cookie material, tokens, the connection string,
  storage keys, and query strings — paths are logged without them. The one deliberate exception is the
  **email address on authentication events**, because a failed-login spike is otherwise unactionable.
  Ordinary writes record nothing about who made them.

Events carry stable numeric ids so queries filter on the id rather than on message text, which is free
to change. Add one in `Api/Logging/Log.cs` — not as a bare `logger.LogInformation` at a call site.

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

Migrations are applied by the pipeline (an EF bundle) **before** the image swaps, so a failed migration
aborts the deploy while the old image is still serving. After ACA is live, `ci.yml` calls
[`web.yml`](.github/workflows/web.yml), which builds the SPA and deploys it to the Cloudflare Worker —
API first, SPA second, so the UI never calls an endpoint that isn't deployed yet.

**Three URLs serve the app:**

| URL | Serves |
|---|---|
| https://app.zionmusicgroup.com | **The front door.** SPA from the edge (~150ms), `/api/*` proxied to ACA |
| https://zmg-tracker.zmg-app.workers.dev | The same Worker on its default hostname — a free second path while validating |
| https://zmg-app.mangohill-c8bd3207.eastus.azurecontainerapps.io | The container serving everything itself — the fallback and rollback target |

The custom domain required moving `zionmusicgroup.com`'s **nameservers** to Cloudflare —
a Workers custom domain only binds to a zone Cloudflare controls. The Netlify marketing site on the
apex and `www` is untouched: those records are **DNS-only (grey cloud)**, so Cloudflare answers the
lookup and Netlify still serves the site and issues its own certificate. Google Workspace mail
(`MX 1 smtp.google.com`) moved across unchanged.

The Worker is an **accelerator, never a dependency**. The container still builds and serves the SPA from
`wwwroot`, so `docker run -e ConnectionStrings__Zmg=… ghcr.io/danielsantil/zmg-tracker:<tag>` always
produces a complete working app. Because ACA scales to zero, a cold start is ~17–22s — the edge means
the UI paints immediately and only the data waits.

- **Rollback / redeploy any build:** Actions tab → **Deploy** → **Run workflow** → enter a prior commit
  SHA. It re-points ACA at that existing image — no rebuild. Note the schema does **not** roll back;
  see [infra/README.md](infra/README.md) → "Migrations and rollback" for when that's safe.
- **Infrastructure changes** go through Terraform in [infra/](infra/README.md), never the pipeline. The
  pipeline owns the image tag; Terraform owns everything else and ignores the tag by design.
- **New required config must reach ACA *before* the image that needs it.** Startup validation fails the
  boot on any missing key, so shipping first means the container won't start and the deploy aborts with
  the old revision still serving — recoverable, but avoidable.
