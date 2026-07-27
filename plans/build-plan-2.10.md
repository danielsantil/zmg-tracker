# ZMG Release Tracker — Build Plan v2.10 (custom domain · authentication · logging)

Delta on [build-plan-2.9.md](build-plan-2.9.md), which shipped M49–M52. Continues milestone numbering
from M52 → **M53–M59**. Branch: `feat/auth-and-logging`.

This is the "before I show it to my partners" plan. Nothing here is a feature the partners will ask
for; everything here is what has to be true before they can be handed a URL. Two of the three
milestones groups are mine to build; the first one and two configuration steps are yours to execute,
with the rationale written out as we go.

## Context

The app is deployed, bilingual, and **completely open**. Anyone who guesses
`zmg-tracker.zmg-app.workers.dev` can read the catalog, archive a release, and delete an artist.
That has been fine while it was a one-person tool on a URL nobody knew. It stops being fine the
moment a link goes out.

It is also **silent**. When something breaks in production the only instrument is
`ContainerAppConsoleLogs_CL` full of unstructured `[boot]` lines and whatever ASP.NET decided to
write. There is no way to ask "what 500'd yesterday", "which endpoint is slow", or "did that upload
actually fail". PROGRESS carries "auth for hosted deploys" and a custom domain as *deferred*; this
plan un-defers both, and adds the observability that makes the first production incident survivable.

Three bodies of work, in dependency order:

1. **A real address** (M53). `app.zionmusicgroup.com` instead of a `workers.dev` subdomain. Already
   researched in full — [`zmg-custom-domain-migration.md`](zmg-custom-domain-migration.md) is the
   execution document and this plan does not restate it.
2. **A door with a lock** (M54–M56). Google SSO, a server-side session, a database whitelist.
3. **Instruments** (M57–M58). Structured JSON logs the API emits, ingress logs Azure emits, one
   correlation id joining them, and a KQL cookbook so "queryable" means something you can copy-paste.

Then M59 verifies the lot and updates the docs.

---

## Locked decisions — don't re-litigate

Four were answered directly before this plan was written; the rest follow from them or from the
existing architecture.

- **Google SSO only. No passwords, no email OTP, no signup.** The domain already runs Google
  Workspace (`MX 1 smtp.google.com`), so every partner has a Google account. One method means: no
  email vendor, no OTP table, no code-expiry window, no per-IP/per-account rate limiting, no
  "resend code" flow, and no password storage — an entire category of security surface that never
  gets built. It also sidesteps NIST 800-63B's position that an inbox is not an authentication
  channel. **If someone without a Google account ever needs in, that is a new milestone**, and the
  note in *Out of scope* says which vendor to reach for.
- **Authorization is flat: on the list or not.** No roles, no per-screen permissions, no admin UI.
  This is why the SPA gets **one gate around the whole app** rather than per-route guards — with a
  single permission level, a route-by-route guard is ceremony that can only ever be wrong in one
  direction.
- **The whitelist is a database table you edit by hand, and it is the *only* whitelist.**
  `AllowedUser`, seeded with your address. Adding a partner is one `INSERT` in Neon — no screen, no
  invite email, no self-service, and **no second list in the Google console** to keep in step. Any
  Google account may authenticate; only listed addresses get in. Authentication is not authorization,
  and keeping those two jobs in one place each is what makes this simple enough to reason about.
- **Sessions live in Postgres and are absolute, not sliding.** 7 days, configurable via
  `Auth:SessionDays`. "Unless invalidated" is why they are server-side rows rather than a
  self-contained cookie: revoking is `DELETE FROM "AuthSessions" WHERE …` and it takes effect on the
  next request. Absolute (`SlidingExpiration = false`) because you said *expiration date*, and
  because a rolling window means a stolen cookie never expires as long as it is being used.
- **Auth events log the email address; nothing else logs who did what.** The exception you approved,
  and it is narrow on purpose: `login.ok`, `login.denied`, `login.failed`, `logout` carry the email.
  Creating a release, editing a task, deleting an artist carry nothing about the actor. A failed-login
  spike is only actionable if you can tell one partner fat-fingering from someone probing the door;
  a business audit trail is a different feature that you did not ask for.
- **The API logs what ingress cannot see; ingress logs the rest.** ACA's Envoy already records every
  request's method, path, status, duration and a `RequestId` — for free, with no app code and no
  latency on the hot path. So the API does **not** log a line per request. It logs errors, warnings,
  auth events, and *only* those requests that were slow or failed. The two sides join on
  `x-request-id`.
- **No new runtime dependency that isn't Microsoft's.** Two packages, both `8.0.*`:
  `Microsoft.AspNetCore.Authentication.OpenIdConnect` and
  `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`. Logging uses the built-in
  `AddJsonConsole` — **no Serilog**. `ILogger` scopes plus `[LoggerMessage]` source generation give
  the same structured output with zero packages, and M41 established that this image stays lean.
- **Cost stays at $0.** Google OAuth is free. Log Analytics is free to 5 GB/month ingestion with 31
  days retention; this app's realistic volume is two orders of magnitude under that, and M58 ships
  the query that proves it rather than assuming it.

---

## How this plan is run

Same solo cadence: **commit after each milestone**, each independently reviewable.

**The split you asked for.** Infrastructure, Terraform, DNS, cloud consoles and secret material are
yours; every line of code is mine. Concretely:

| Milestone | Yours | Mine |
|---|---|---|
| M53 custom domain | All of it (DNS, Cloudflare, Namecheap) | `worker.ts` + `wrangler.jsonc` edits |
| M54 auth schema | — | Everything |
| M55 auth API | Google Cloud Console app; the two ACA secrets; dev user-secrets | Everything else |
| M56 auth SPA | — | Everything |
| M57 app logging | — | Everything |
| M58 ingress logs | Terraform `logs_destination` + diagnostic setting | The KQL cookbook |
| M59 verification | Prod smoke test | Full suite, browser verification, docs |

Where a step is yours, this plan explains **why**, not just what — that is the point of doing them.

**Blast radius** (per CLAUDE.md):

- **M53** — infra + two SPA config files. `pnpm build` only.
- **M54, M55, M57** — entities, migrations, DTOs, endpoints → **full `dotnet test`**.
- **M56** — SPA only → `pnpm lint && pnpm test && pnpm build`. No `dotnet test`.
- **M58** — Terraform + a markdown file → no verification.
- **M59** — everything, plus live browser verification.

v2.9's prod schema reset was completed and deployed before this plan started, so M54's migration is a
normal additive one on top of the squashed `InitialCreate` — no destructive prerequisite, and rollback
across it is a normal expand-only case.

---

## M53 — `app.zionmusicgroup.com`

**The plan already exists.** [`zmg-custom-domain-migration.md`](zmg-custom-domain-migration.md) is
complete, verified against the live nameservers, and I am not going to paraphrase it here. Execute it
phase by phase; I will answer questions and run the verification commands with you as you go.

Three things this plan **adds** to that document, because auth changes its consequences:

### 53a. The Worker must forward the original Host — and this is load-bearing for M55

Today `worker.ts` does:

```ts
return fetch(new Request(new URL(url.pathname + url.search, env.API_ORIGIN), request));
```

That rewrites the URL, so the request arriving at the container has
`Host: zmg-app.…azurecontainerapps.io` — not `app.zionmusicgroup.com`. It has to: ACA's ingress routes
on `Host` and would reject anything else.

Harmless until now, **fatal for OAuth**. ASP.NET builds the OIDC `redirect_uri` from
`Request.Scheme` + `Request.Host`. Unfixed, the API would send Google a `redirect_uri` pointing at the
ACA FQDN, Google would reject it as unregistered, and login would fail with an error that names
neither the Worker nor the Host header. So the Worker gains one header:

```ts
const headers = new Headers(request.headers);
headers.set('X-Forwarded-Host', url.host);   // app.zionmusicgroup.com
headers.set('X-Forwarded-Proto', url.protocol.replace(':', ''));
```

and the API reads it back with `UseForwardedHeaders` (M55). **Constrained by an allow-list**, not by
trusting the proxy: the ACA FQDN stays publicly reachable, so without `AllowedHosts` anyone could
forge `X-Forwarded-Host` and make the app believe it lives on their domain. `AllowedHosts` reduces
that to "the two hostnames we actually serve" and the whole class of attack disappears.

### 53b. Register every origin in Google, including the rollback path

`https://<aca-fqdn>/…` must be an authorized redirect URI too. Otherwise the ACA URL — which M42
established as the rollback target and which the Dockerfile keeps serving `wwwroot` for precisely so
it stays one — becomes a URL you cannot log into. A rollback target you can't authenticate against is
not a rollback target. Same for `workers.dev` while §6a keeps it alive.

### 53c. `img.zionmusicgroup.com` stays deferred

§8 of the migration doc is still a good idea and still out of scope. Cover URLs are persisted
absolute in the database, so switching `R2__PublicBaseUrl` needs a data migration. Not this plan.

**Acceptance:** the migration doc's own Phase 3 and Phase 5 checklists, unchanged — including the
real test email, which is the one failure you would not otherwise notice.

---

## M54 — Auth schema + pure domain rules

**Entities** (`src/Zmg.Domain/Entities/`):

```csharp
AllowedUser   Id, Email (unique, normalized), DisplayName?, CreatedAt, DisabledAt?
AuthSession   Id (string PK), AllowedUserId, Email, TicketData (byte[]),
              CreatedAt, ExpiresAt, LastSeenAt?
```

`AllowedUser.DisabledAt` exists so revoking access is reversible and leaves a trace, rather than a
`DELETE` that loses the fact the person was ever there. `AuthSession.Email` is denormalized — a
revoked session should still be attributable when you are reading the table to answer "who is logged
in right now", without a join, and it survives the user row being edited.

**Pure domain logic** (`src/Zmg.Domain/`), unit-tested, no I/O — the layer rule holds:

- `EmailNormalization.Normalize(string)` — trim + `ToLowerInvariant`. Stored normalized with a unique
  index, compared with plain ordinal `==` so the SQLite tests stay representative of Postgres
  (the v2.5 provider-agnostic rule).
- `AccessControl.IsAllowed(AllowedUser?)` — non-null and `DisabledAt is null`. One expression, one
  place, one test. Every deny path in the API calls this rather than re-deriving it.
- `Redirects.SafeLocalPath(string?)` — the open-redirect guard for `?returnUrl=`. Must start with a
  single `/`, must not start with `//` or `/\`, falls back to `/`. This is the kind of thing that is
  three lines and gets it wrong three ways; it gets its own tests.

**Data protection keys** — `ZmgDbContext` implements `IDataProtectionKeyContext` and gains
`DbSet<DataProtectionKey>`.

> **Why this is not optional.** ASP.NET encrypts the session cookie with Data Protection keys. By
> default those keys live on the container filesystem, which on ACA is **ephemeral**. With
> `min_replicas = 0` and a 300s cooldown, the replica dies after five minutes of inactivity and the
> next one generates fresh keys — so *every* cold start would silently log everyone out, as would
> every deploy. Persisting the key ring to Postgres is what makes a 7-day session mean 7 days.
> This is the single most likely way this feature would have shipped "working" and then been
> mysteriously broken in production.

**Migration:** one additive `AddAuthentication` migration — three tables, a unique index on
`AllowedUser.Email`, an index on `AuthSession.ExpiresAt` for the cleanup sweep.

**Seed:** your email into `AllowedUser`. Everyone else is an `INSERT` you run.

**Tests:** `EmailNormalizationTests`, `AccessControlTests`, `RedirectsTests` — pure, fast, in
`Zmg.Domain.Tests`. Expect **domain 125 → ~140**.

---

## M55 — Auth API

### The flow

```
Browser                    API (ACA)                     Google
  │  GET /api/auth/login?returnUrl=/releases
  ├──────────────────────────►│
  │  302 → accounts.google.com (code + PKCE + state + nonce)
  │◄──────────────────────────┤
  ├───────────────────────────────────────────────────────►│
  │  302 → /api/auth/google/callback?code=…&state=…        │
  │◄───────────────────────────────────────────────────────┤
  ├──────────────────────────►│
  │                           │  token exchange, id_token validated
  │                           ├───────────────────────────►│
  │                           │  email + email_verified
  │                           │
  │                           │  AllowedUser lookup ──► allowed?
  │                           │     no  → log auth.login.denied, 302 /login?denied=1
  │                           │     yes → INSERT AuthSession, log auth.login.ok
  │  302 → /releases  + Set-Cookie: zmg_session=<opaque>; HttpOnly; Secure; SameSite=Lax
  │◄──────────────────────────┤
```

**Handler: `AddOpenIdConnect` against `https://accounts.google.com`**, not `AddGoogle`. Google's
OIDC discovery document means PKCE (`UsePkce` defaults to `true` on .NET 8), the `nonce`, `state`, and
full `id_token` signature/issuer/audience validation are all handled by Microsoft's code. The email
arrives inside the validated `id_token`; there is no userinfo round-trip and no place for me to
hand-roll a JWT check. This is a BFF: tokens never reach JavaScript, the browser only ever holds an
opaque session id.

### Configuration

| Key | Dev | Prod |
|---|---|---|
| `Authentication:Google:ClientId` | user-secrets | ACA secret |
| `Authentication:Google:ClientSecret` | user-secrets | ACA secret |
| `Auth:SessionDays` | `7` | `7` |
| `Auth:AllowedHosts` | `localhost` | `app.zionmusicgroup.com`, workers.dev, ACA FQDN |
| `Auth:PostLoginOrigin` | `http://localhost:5173` | *(empty)* |

`Configuration.Validate()` (M35's fail-fast) grows the two Google keys, so a deploy missing them dies
at boot naming them — rather than booting fine and failing on the first login attempt.

> **`Auth:PostLoginOrigin` is a dev-loop fix, and worth understanding.** In dev the SPA is on
> `:5173` and the API on `:5274`; Vite proxies `/api`. Google redirects to the *server's* callback, so
> the browser lands on `:5274` and would then be served the API's `wwwroot` copy of the SPA — stale,
> or absent. Cookies ignore port (they're host-scoped), so the session itself is fine; only the
> landing page is wrong. This setting sends you back to `:5173` after login. Empty in prod, where
> there is one origin.

### Cookie

```csharp
Cookie.Name         = "zmg_session";
Cookie.HttpOnly     = true;                 // JS can never read it
Cookie.SecurePolicy = prod ? Always : SameAsRequest;   // dev is http://localhost
Cookie.SameSite     = SameSiteMode.Lax;     // Strict would break the return from Google
ExpireTimeSpan      = TimeSpan.FromDays(Auth:SessionDays);
SlidingExpiration   = false;
SessionStore        = <PostgresTicketStore>;
```

`SameSite=Lax` is also the CSRF control: it stops a cross-site form or `fetch` from carrying the
session cookie on a state-changing request. Combined with an API that only accepts
`application/json` bodies, there is no antiforgery token to add.

**`PostgresTicketStore : ITicketStore`** — `StoreAsync` serializes the ticket with
`TicketSerializer.Default`, writes an `AuthSession` row keyed by a fresh GUID, and returns that key;
the cookie carries only the key. `RemoveAsync` deletes the row, which is what makes logout and manual
revocation instant.

> **Trap: the ticket store is effectively a singleton, `ZmgDbContext` is scoped.** Injecting the
> DbContext directly captures a disposed context and fails on the second request. The store takes
> `IServiceScopeFactory` and opens its own scope per call. Also: `StoreAsync` runs only at login
> (rare), so it is the right place to opportunistically `DELETE` expired rows — no background service
> needed, and with scale-to-zero a timer would barely run anyway.

`AddDataProtection().PersistKeysToDbContext<ZmgDbContext>().SetApplicationName("zmg-tracker")` —
`SetApplicationName` is pinned because the default derives from the content-root path, which differs
between the container and your laptop, and a mismatch silently invalidates every cookie.

### Endpoints — `AuthEndpoints.cs`

| Route | Behaviour |
|---|---|
| `GET /api/auth/login?returnUrl=` | `Results.Challenge` → Google. `returnUrl` through `Redirects.SafeLocalPath`. Anonymous. |
| `GET /api/auth/google/callback` | The handler's `CallbackPath`. Whitelist check in `OnTicketReceived`. Anonymous. |
| `GET /api/auth/me` | `{ email, displayName }` or 401. Anonymous (it *is* the auth probe). |
| `POST /api/auth/logout` | `SignOutAsync` → row deleted → 204. |

The whitelist check lives in `OnTicketReceived` and does three things: requires
`email_verified == true`, looks the normalized email up through `AccessControl.IsAllowed`, and — on
success — **replaces the principal with a minimal `ClaimsPrincipal` carrying email and name only**.
Google's full token payload does not get serialized into the session; the session stores what the app
needs, which is two strings.

### Everything else requires authentication, by default

```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
```

A fallback policy means a new endpoint is **protected unless it opts out**. Adding
`MapWhateverEndpoints()` in six months and forgetting `.RequireAuthorization()` is then a non-event
instead of a hole. Explicit `.AllowAnonymous()`: `/api/health`, the auth endpoints, and
`MapFallbackToFile("index.html")` — the SPA shell stays public because it has to render the login
screen. Static files aren't endpoint-routed and are unaffected.

`OnRedirectToLogin` is overridden: `/api/*` gets a **401 with `{"errors":[{"code":"error.auth.required"}]}`**,
not a 302 to an HTML login page. An XHR that follows a redirect and parses HTML as JSON is a
confusing failure; a 401 is the SPA's signal to show the gate.

**New message codes** — `error.auth.required`, `error.auth.notAllowed`, `error.auth.emailUnverified`.
Per M46 they need keys in **both** `en.json` and `es.json` or `MessageCodeApiTests` fails.

### Your configuration steps (M55)

1. **Google Cloud Console → Google Auth Platform → Audience.** (The old "OAuth consent screen"; it
   now lives under *APIs & Services → Google Auth Platform*, split into Branding / Audience /
   Clients.) Set the audience to **External** and **publish it — status "In production"**. Do not
   maintain a test-user list.

   **Any Google account may authenticate. `AllowedUser` is the only gate.** That includes
   `@zionmusicgroup.com` Workspace accounts, personal `@gmail.com` accounts, and accounts on a
   partner's own domain — all identical as far as this app is concerned.

   > **Why not Internal.** Internal restricts sign-in to members of the `zionmusicgroup.com`
   > Workspace organization, so anyone on `@gmail.com` or their own company domain is rejected by
   > Google with `org_internal` before the request reaches this app. It would also require the Cloud
   > project to sit inside that organization. Rejected: it excludes exactly the people this is for.
   >
   > **Why not External/Testing.** A test-user list would be a second allow-list to keep in step with
   > `AllowedUser` — adding a partner would mean two edits in two consoles, and forgetting the Google
   > one produces a confusing failure *before* the app can say anything useful. One list, one place.
   >
   > **Publishing costs nothing and requires no review.** Verification is only required for
   > *sensitive* or *restricted* scopes. This app requests `openid`, `email` and `profile` — the
   > basic profile scopes — so there is no review, no "unverified app" interstitial, and no 100-user
   > cap. Publishing is a single confirmation in the console.

   **What "publicly reachable login" does and does not mean.** Anyone on the internet can now reach
   the Google consent screen for this client and come back with a validated identity. That is fine,
   and it is the normal shape of SSO: authentication is not authorization. What they get is the
   denied screen and an `auth.login.denied` log line. Nothing about the app, its data, or the
   whitelist is disclosed — the denied screen deliberately gives no hint about which addresses
   *would* work (M56). The security boundary is `AllowedUser`, it lives in your database, and it is
   revocable in one statement.
2. **Create an OAuth 2.0 Web application client.** Authorized redirect URIs — all of them:
   `https://app.zionmusicgroup.com/api/auth/google/callback`,
   `https://zmg-tracker.zmg-app.workers.dev/api/auth/google/callback`,
   `https://<aca-fqdn>/api/auth/google/callback` (§53b),
   `http://localhost:5274/api/auth/google/callback` (dev).
3. **Client secret → two places, never a third.** `dotnet user-secrets` in `src/Zmg.Api` for dev; an
   **ACA secret** referenced by `env` in `infra/azure.tf` for prod, exactly like `neon-conn` and the
   R2 keys. The value goes in `terraform.tfvars` (gitignored) as a new
   `var.google_client_secret`; it never appears in a `.tf` file, a commit, or a GitHub secret.

**Tests.** `ZmgApiFactory` gains an `Authenticated` flag defaulting to `true`, which registers a test
authentication scheme — so all **214 existing API tests keep passing untouched**. New
`AuthApiTests` constructs an unauthenticated factory and pins: an anonymous request to a business
endpoint is 401 with the code (not a 302); `/api/health` and `/api/auth/me` stay reachable; a
non-whitelisted email is denied; a disabled user is denied; `SafeLocalPath` rejects an absolute
`returnUrl` end to end. Expect **API 214 → ~230**.

---

## M56 — Auth SPA: the login screen

### One gate, not per-route guards

Flat authorization means there is nothing to guard *per route*. `AuthProvider` runs
`GET /api/auth/me` once (`retry: false`, `staleTime: Infinity`) and `AuthGate` renders one of three
things for the entire app: a splash while it resolves, the login screen when it 401s, the existing
`NavBar` + `Routes` when it succeeds. The URL is untouched throughout, so after login the browser is
already where the user was trying to go and `returnUrl` needs no client-side bookkeeping.

`client.ts` gains one branch: a 401 invalidates `['auth','me']`, which flips the gate to the login
screen. Expired-session handling is that single line — no interceptor chain, no refresh loop.

### Wireframes

```
┌──────────────────────────────────────────────────┐   Desktop / ≥sm
│                                        [ES] [☾]  │   token colours only:
│                                                  │   bg-ink, panel, edge,
│              ╔══════════════════════╗            │   strong/body/muted
│              ║   [ ZMG wordmark ]   ║            │
│              ║                      ║            │   card: bg-panel,
│              ║   Release Tracker    ║            │   border-edge, rounded-xl,
│              ║   Sign in to continue║            │   max-w-sm, centred
│              ║                      ║            │
│              ║  ┌────────────────┐  ║            │   Google button follows
│              ║  │ G  Continue    │  ║            │   Google's branding rules:
│              ║  │    with Google │  ║            │   official mark, neutral
│              ║  └────────────────┘  ║            │   surface, unaltered text
│              ║                      ║            │
│              ║  Internal tool.      ║            │
│              ║  Access is by invite ║            │
│              ╚══════════════════════╝            │
└──────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────┐   Denied  (?denied=1)
│              ╔══════════════════════╗            │
│              ║   [ ZMG wordmark ]   ║            │   amber, not red — this is
│              ║   ⚠ Not on the list  ║            │   "wrong door", not "error".
│              ║                      ║            │   Red is reserved for
│              ║  someone@gmail.com   ║            │   destructive actions (M16).
│              ║  isn't authorised    ║            │
│              ║  for this workspace. ║            │   No hint about which
│              ║                      ║            │   addresses *would* work —
│              ║  ┌────────────────┐  ║            │   that's an enumeration
│              ║  │ Use another    │  ║            │   oracle for free.
│              ║  │ account        │  ║            │
│              ║  └────────────────┘  ║            │
│              ╚══════════════════════╝            │
└──────────────────────────────────────────────────┘

 375px                          NavBar, authenticated (≥sm)
┌────────────────────┐         ┌──────────────────────────────────────────┐
│          [ES] [☾]  │         │ [ZMG] Home Releases Catalog … [ES][☾][◍] │
│                    │         └──────────────────────────────────────────┘
│  [ ZMG wordmark ]  │              account popover ─┐  (portals to <body>,
│                    │                     ┌─────────▼──────────┐  per the
│  Release Tracker   │                     │ daniel@zionmusic…  │  v2.2 rule)
│  Sign in to continue│                    │ ────────────────── │
│                    │                     │ ⏻  Sign out        │
│ ┌────────────────┐ │                     └────────────────────┘
│ │ G  Continue    │ │
│ │    with Google │ │         Below sm the same two rows live at the bottom
│ └────────────────┘ │         of the existing hamburger sheet — no second
│                    │         popover implementation.
│ Internal tool.     │
│ Access is by invite│
└────────────────────┘
```

The card sits on the same `bg-panel` / `border-edge` / `rounded-xl` vocabulary as `Modal`, and both
toggles are the existing components — the login screen introduces **no new visual primitives**. It is
deliberately the plainest screen in the app: one button, no form, nothing to get wrong.

`LanguageToggle` and `ThemeToggle` are present *before* login because both preferences live in
`localStorage` and both are read pre-paint; hiding them would mean the first thing a Spanish-speaking
partner sees is English with no way out.

### Also in M56

- `NavBar` gains the account control — a `RowMenu`-style popover (portalled to `<body>`, v2.2 rule)
  with the email and **Sign out**; below `sm` it's two rows at the foot of the existing sheet.
- All new strings keyed into **both** `en.json` and `es.json`; `i18n.test.ts` enforces parity.
- `error.auth.*` codes render through the existing `serverText.ts` path — no new mechanism.

**Tests:** the repo has no component tests and no `@testing-library/react`; adding it for this would
be as out of step as it was in M51. What gets tested is the pure part —
`lib/returnUrl.ts` mirroring `SafeLocalPath` client-side, and the gate's state derivation. The screens
themselves are verified in the browser (M59). Expect **web 57 → ~63**.

---

## M57 — Structured application logs

### Format

`AddJsonConsole` in non-Development (`IncludeScopes = true`, `UseUtcTimestamp = true`,
`Indented = false`); the readable `SimpleConsole` stays in dev. One JSON object per line to stdout →
ACA picks it up → `ContainerAppConsoleLogs_CL.Log_s` → `parse_json` in KQL. No sink, no package, no
network call, nothing to fail closed.

### Levels — the difference between "detailed" and "verbose"

```jsonc
"LogLevel": {
  "Default": "Information",
  "Zmg": "Information",
  "Microsoft.AspNetCore": "Warning",
  "Microsoft.EntityFrameworkCore": "Warning",       // else every SQL statement, every request
  "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
}
```

EF's command logger at `Information` alone would be the bulk of the ingestion and none of the signal.
`Microsoft.AspNetCore` at `Warning` drops the per-request start/finish pair that ingress already
records better.

### What gets added

- **`CorrelationMiddleware`** — reads `x-request-id` (Envoy sets it at ACA ingress; generates one if
  absent), pushes `ILogger.BeginScope(new { RequestId })` so *every* line in that request carries it,
  and echoes it as a response header. This is the join key: an app log line and its ingress record
  share it, and a partner can quote the id off an error screen.
- **`GlobalExceptionHandler : IExceptionHandler`** — logs the exception once at `Error` with the
  request id, method and path, then returns a 500 carrying `{"errors":[{"code":"error.unexpected"}]}`
  and the request id. Today an unhandled exception produces framework-default output and an
  inconsistent body; this makes 500s both queryable and consistent with M46's code scheme.
- **`RequestSummaryMiddleware`** — one `Information` line per request, **only when** status ≥ 400 or
  elapsed ≥ `Logging:SlowRequestMs` (default 1000). Method, path *without query string*, status,
  duration, request id. The happy path stays silent because ingress already has it.
- **`Zmg.Api.Logging.Log`** — a `partial class` of `[LoggerMessage]` source-generated methods. Named,
  numbered event ids (`1000` auth, `2000` uploads, `3000` requests) mean KQL can filter on
  `EventId == 1001` instead of matching message text, and the generator makes them allocation-free.

**Events worth an `Information` line, and no others:**

| Event | Id | Fields |
|---|---|---|
| `auth.login.ok` | 1000 | email, session id |
| `auth.login.denied` | 1001 | email, reason (`not_listed` / `disabled` / `email_unverified`) |
| `auth.logout` | 1002 | email |
| `cover.upload` | 2000 | outcome, source bytes → stored bytes, ms |
| `cover.fetch.blocked` | 2001 | reason (the M31 SSRF guards) |
| `request.slow` / `request.failed` | 3000/3001 | method, path, status, ms |
| `[boot] …` | — | already exists, unchanged |

**Never logged, and this is a rule not a preference:** the session cookie or ticket id material, the
Google client secret, tokens of any kind, the connection string, R2 keys, and query strings (ACA's own
HTTP logs warn that `Path` can carry secrets when clients put them in the query — ours don't, and
logging without the query keeps it that way).

**Tests:** `RedirectsTests`-style pure coverage where there is pure logic; API tests pin that a
handler throwing produces a 500 with `error.unexpected` and an `x-request-id` response header, and
that a request id supplied by the caller is echoed rather than replaced. Expect **API ~230 → ~236**.

---

## M58 — Ingress logs + the KQL cookbook

### The infrastructure change, and its one real risk

`ContainerAppHTTPLogs` — method, path, status, duration, `RequestId`, client IP, per request, emitted
by Envoy at no CPU cost to the app — is **only** available when the environment's logs destination is
`azure-monitor` rather than `log-analytics`. Switching has consequences worth understanding before you
run it:

- Console and system logs move from the custom tables `ContainerAppConsoleLogs_CL` /
  `ContainerAppSystemLogs_CL` to the resource-specific `ContainerAppConsoleLogs` /
  `ContainerAppSystemLogs`. Same data, better-typed columns, **different table names** — the cookbook
  ships both spellings for this reason.
- The container app's portal **Logs** blade goes away; you query from the workspace instead.
- Diagnostic settings become the thing that routes logs, so forgetting to create one means logs go
  *nowhere*. Order matters: set the destination, then immediately create the setting.

> ⚠️ **Stop and read the plan output.** In `azurerm`, changing an environment's logging configuration
> has historically been `ForceNew`. Replacing `azurerm_container_app_environment` would recreate the
> container app and **change its FQDN**, which is hard-coded as `API_ORIGIN` in `wrangler.jsonc` and
> registered as a Google redirect URI. Per `infra/README.md`'s standing rule: **if `terraform plan`
> says `forces replacement`, the answer is no.** Ingress logs are a convenience; a stable origin is
> not. If it forces replacement, skip the switch — M57's `request.slow` / `request.failed` lines
> already cover the failure cases, and the cookbook's app-log queries all still work.

Terraform: `logs_destination = "azure-monitor"` on `azurerm_container_app_environment`, plus an
`azurerm_monitor_diagnostic_setting` on the environment targeting the existing workspace with the
`ContainerAppConsoleLogs`, `ContainerAppSystemLogs` and `ContainerAppHTTPLogs` categories. If the
provider hasn't caught up on the HTTP category, `azapi_resource` is the documented fallback — but try
plain `azurerm` first.

Retention stays at the current 30 days (`retention_in_days = 30` in `azure.tf`), inside the free
31-day window.

### `docs/kql-cookbook.md`

The actual deliverable of this milestone: queries you copy-paste, each with a one-line "use this
when". Sketch of the contents —

```kusto
// Everything the app said, parsed, newest first
ContainerAppConsoleLogs_CL
| where ContainerAppName_s == "zmg-app" and TimeGenerated > ago(24h)
| extend p = parse_json(Log_s)
| mv-apply s = p.Scopes on (
    where isnotempty(s.RequestId) | project RequestId = tostring(s.RequestId))
| project TimeGenerated,
          Level    = tostring(p.LogLevel),
          Event    = toint(p.EventId),
          Category = tostring(p.Category),
          Message  = tostring(p.Message),
          RequestId,
          Exception= tostring(p.Exception)
| order by TimeGenerated desc
```

```kusto
// Errors only, grouped — "what is actually broken", not "what happened"
… | where Level in ("Error", "Critical")
  | summarize Count = count(), First = min(TimeGenerated), Last = max(TimeGenerated),
              Sample = take_any(Message) by Category, Exception
  | order by Count desc
```

```kusto
// Failed logins by address — the one query the email exception exists for
… | where Event == 1001
  | summarize Attempts = count(), Reasons = make_set(Reason) by Email, bin(TimeGenerated, 1h)
  | where Attempts > 3
```

```kusto
// One request, end to end: ingress record + every app line that shares its id
let rid = "<paste from the error screen>";
union (ContainerAppHTTPLogs | where RequestId == rid),
      (ContainerAppConsoleLogs_CL | extend p = parse_json(Log_s) | where Log_s has rid)
| order by TimeGenerated asc
```

Plus: slowest endpoints (p95 by path), 5xx rate by revision after a deploy, cover-upload outcomes,
cold-start boot timings (the existing `[boot]` lines, finally queryable), and —

```kusto
// Am I still free? 5 GB/month is the allowance.
Usage | where TimeGenerated > ago(30d) | summarize GB = sum(Quantity) / 1000 by DataType
     | order by GB desc
```

> **The scopes parse is the fragile part.** `AddJsonConsole` emits `Scopes` as a heterogeneous array,
> so `p.Scopes[1].RequestId` would break the moment another scope is pushed. `mv-apply` finds the
> right element regardless of position. Written down because it is not obvious and it is the query
> everything else builds on.

---

## M59 — Verification + docs

Full `dotnet test`, then `pnpm lint && pnpm test && pnpm build`, then live browser verification
against real Postgres in the production-style single process, then prod.

1. Anonymous visit → login screen. No page of the app renders, and the network tab shows a **401**
   from `/api/auth/me`, not a redirect.
2. Sign in with a whitelisted Google account → land on the page you were trying to reach, not `/`.
3. Sign in with a non-whitelisted account → the denied screen, with `auth.login.denied` in the log
   carrying the email and `not_listed`.
4. `DELETE` the session row in Neon while the tab is open → the next action drops to the login
   screen. This is the "unless invalidated" requirement, proven.
5. Set `Auth:SessionDays=0.001`, wait, confirm expiry; set it back. Proves *configurable*, which is
   otherwise a claim nobody checks.
6. **Restart the container** (or let it scale to zero and come back) → **still logged in**. This is
   the Data Protection key persistence test and the one most likely to fail.
7. Sign out → cookie gone, row gone, `auth.logout` logged.
8. Force a 500 → the SPA shows a coded message with a request id, and that exact id finds both the
   app log line and (if M58 landed) the ingress record.
9. `curl https://<aca-fqdn>/api/releases` directly → 401. The origin is not a bypass.
10. Every screen still works logged in — this plan touches `Program.cs` and the shell, so the point is
    to prove it changed nothing else.

**Docs:**
- `plans/PROGRESS.md` — v2.10 journal entry, backlog updated, new cross-cutting bullets (fallback
  authorization policy, DP keys in Postgres, the forwarded-host rule, the auth-events-only logging
  exception).
- `CLAUDE.md` — auth + logging conventions; "adding an endpoint" grows "it is protected by default".
- `README.md` — the URL table gets `app.zionmusicgroup.com`; a "who can log in" section; how to add a
  partner (the `INSERT`).
- `infra/README.md` — the Google client secret joins the documented secret inventory; the logging
  destination decision and its outcome recorded either way.
- `docs/kql-cookbook.md` — new, from M58.

---

## Out of scope

- **Email OTP / magic links.** Deliberately deferred, not forgotten. If it's ever needed: **Resend**
  (3,000/month free, SPF+DKIM as Cloudflare TXT records — trivial once M53 lands, and it would start
  paying down the missing-SPF exposure that §9 of the migration doc flags). Gmail SMTP with an app
  password is the no-new-vendor alternative, at the cost of a long-lived credential tied to one
  mailbox.
- **Roles, permissions, per-screen access.** Flat by decision.
- **A user-management screen.** The whitelist is a table.
- **Audit trail / user attribution on business writes.** Explicitly excluded. Note that adding it
  later is cheap *now* — the session already knows the email — and expensive if the entities have to
  grow `CreatedBy` columns retroactively.
- **Alerting.** Azure Monitor alert rules are billed per rule; the cookbook is the manual substitute.
  Revisit if $0 stops being the constraint.
- **Passkeys / WebAuthn.** Google handles the second factor already; ASP.NET Identity's passkey
  support would mean adopting Identity, which this plan avoids entirely.
- **`img.zionmusicgroup.com`** (§8 of the migration doc) — needs a cover-URL data migration.
- **Phase 2 (DSP stats), real-Postgres tests, per-track fan-out** — still `build-plan-3.0.md`.
