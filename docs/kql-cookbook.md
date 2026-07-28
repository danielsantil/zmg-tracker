# KQL cookbook

Queries for the ZMG Release Tracker's production logs. Paste one in, change the time window, get an
answer — this is a reference for someone who touches KQL four times a year, not a tutorial.

The logging design and its rules live in [`plans/PROGRESS.md`](../plans/PROGRESS.md) (v2.10/M57–M58).
This file is only about asking questions of what came out.

## Where to run these

**Portal** → Log Analytics workspace **`workspace-zmgrgxjgf`** (resource group `zmg-rg`) → **Logs**.

Note the container app's own **Logs** blade no longer works — that is expected. Switching the
environment to the `azure-monitor` logs destination (M58) is what unlocked `ContainerAppHTTPLogs`, and
losing the per-app blade is the documented price. Query the workspace instead.

From a terminal:

```bash
az monitor log-analytics query \
  --workspace 0445fea0-489b-47ff-b641-350fd535ede1 \
  --analytics-query "ContainerAppHTTPLogs | where TimeGenerated > ago(1h) | take 5" \
  -o table
```

**Retention is 30 days** (`retention_in_days = 30` on the workspace), inside the free 31-day window.
Anything older is gone — if a number matters beyond a month, write it down somewhere else.

## The two tables

| Table | Written by | One row per |
|---|---|---|
| `ContainerAppHTTPLogs` | Envoy, at the ACA ingress | every HTTP request, at no cost to the app |
| `ContainerAppConsoleLogs` | the app's stdout | every log line the app chose to write |

They are complements, not duplicates, and the split is deliberate: **ingress already records every
request better than the app can**, so the app stays silent on the happy path and speaks only when it
has something ingress cannot know — a stack trace, an auth decision, a byte count. Twenty ingress rows
with no matching app rows is the system working, not a gap.

They join on the request id. See [One request, end to end](#one-request-end-to-end).

> **Data before 2026-07-28 is in different tables.** The M58 switch moved console and system logs from
> the custom tables `ContainerAppConsoleLogs_CL` / `ContainerAppSystemLogs_CL` to the resource-specific
> ones above. Nothing was migrated. Older rows are still queryable under the old names, where every
> column carries an `_s` suffix (`Log_s`, `ContainerAppName_s`, `RevisionName_s`) and there is no HTTP
> log at all. See [Querying history](#querying-history-before-the-switch).

## The base query

Everything app-side builds on this. Run it alone first to confirm rows are flowing, then bolt the
recipes onto it.

```kusto
ContainerAppConsoleLogs
| where TimeGenerated > ago(24h) and ContainerAppName == "zmg-app"
| where Log startswith "{"                       // JSON only; see the gotcha on old revisions
| extend p = parse_json(Log)
| extend
    Level         = tostring(p.LogLevel),
    Event         = toint(p.EventId),
    Category      = tostring(p.Category),
    Msg           = tostring(p.Message),
    Exception     = tostring(p.Exception),
    CorrelationId = extract(@'"CorrelationId":"([^"]+)"', 1, Log)
```

`CorrelationId` is pulled with `extract` rather than by indexing `p.Scopes`, deliberately — see
[the scopes gotcha](#the-scopes-array-is-the-fragile-part).

---

## Recipes

### Everything the app said

Use this when: you have no idea yet, and want to read.

```kusto
ContainerAppConsoleLogs
| where TimeGenerated > ago(24h) and ContainerAppName == "zmg-app"
| where Log startswith "{"
| extend p = parse_json(Log)
| project TimeGenerated,
          Level         = tostring(p.LogLevel),
          Event         = toint(p.EventId),
          Category      = tostring(p.Category),
          Message       = tostring(p.Message),
          CorrelationId = extract(@'"CorrelationId":"([^"]+)"', 1, Log),
          Exception     = tostring(p.Exception),
          RevisionName
| order by TimeGenerated desc
```

### Errors only, grouped

Use this when: you want *what is broken*, not *what happened*. Ten thousand lines collapse to a
handful of rows, each with a sample request id to pull the full story with.

```kusto
ContainerAppConsoleLogs
| where TimeGenerated > ago(7d) and ContainerAppName == "zmg-app"
| where Log startswith "{"
| extend p = parse_json(Log)
| where tostring(p.LogLevel) in ("Error", "Critical")
| extend Exception     = tostring(p.Exception),
         CorrelationId = extract(@'"CorrelationId":"([^"]+)"', 1, Log)
| extend ExceptionType = extract(@"^([\w\.]+):", 1, Exception)
| summarize Count   = count(),
            First   = min(TimeGenerated),
            Last    = max(TimeGenerated),
            Sample  = take_any(tostring(p.Message)),
            TraceId = take_any(CorrelationId)
        by Category, ExceptionType
| order by Count desc
```

### One request, end to end

Use this when: a partner quotes you a reference off an error screen, or you have an id from an
`x-request-id` response header. This is the query the whole scheme exists for — the ingress record and
every app line for the same request, interleaved.

```kusto
let rid = "PASTE-THE-ID-HERE";
let window = 24h;
union
  (ContainerAppHTTPLogs
   | where TimeGenerated > ago(window) and RequestId == rid
   | project TimeGenerated,
             Source = "ingress",
             Detail = strcat(Method, " ", Path, " -> ", StatusCode, " (", RequestDuration, " ms)")),
  (ContainerAppConsoleLogs
   | where TimeGenerated > ago(window) and Log contains rid
   | extend p = parse_json(Log)
   | project TimeGenerated,
             Source = "app",
             Detail = strcat(tostring(p.LogLevel), " [", tostring(p.EventId), "] ", tostring(p.Message)))
| order by TimeGenerated asc
```

**Why this works:** ACA's Envoy stamps `x-request-id` on the inbound request and logs it as
`RequestId`; `CorrelationMiddleware` adopts that same value rather than minting its own, publishes it
as `CorrelationId` on every app line of the request, and echoes it back on the response. Verified
against a live 401 on 2026-07-28: the header, the ingress row and the app row all carried one id.

`contains` rather than `has` is on purpose — `has` matches whole terms, and a dashed UUID gets
tokenized in ways that make it miss.

### Failed logins by address

Use this when: someone can't get in, or you want to know whether anyone is trying. **This is the query
the email-logging exception exists for** — auth events are the one place the app records who, because
a failed-login spike is otherwise unactionable.

```kusto
ContainerAppConsoleLogs
| where TimeGenerated > ago(7d) and ContainerAppName == "zmg-app"
| where Log startswith "{"
| extend p = parse_json(Log)
| where toint(p.EventId) == 1001
| extend Email = tostring(p.State.Email), Reason = tostring(p.State.Reason)
| summarize Attempts = count(), Reasons = make_set(Reason), Last = max(TimeGenerated)
        by Email, Hour = bin(TimeGenerated, 1h)
| where Attempts > 3
| order by Attempts desc
```

`Reason` is `not_listed`, `disabled` or `email_unverified`. The *browser* is told none of that — one
code for all three, because distinguishing them is a membership oracle. The distinction exists only
here, where whoever is reading is already trusted.

### Who signed in

Use this when: you want to know the app is actually being used, or you need a session id to revoke.

```kusto
ContainerAppConsoleLogs
| where TimeGenerated > ago(30d) and ContainerAppName == "zmg-app"
| where Log startswith "{"
| extend p = parse_json(Log)
| where toint(p.EventId) in (1000, 1002)                 // login.ok, logout
| project TimeGenerated,
          Event     = iff(toint(p.EventId) == 1000, "login", "logout"),
          Email     = tostring(p.State.Email),
          SessionId = tostring(p.State.SessionId)
| order by TimeGenerated desc
```

The session id is the `AuthSession` row key — revoking that session is a `DELETE` against exactly that
id. It is not a bearer token: the browser only ever holds it inside a Data-Protection-encrypted
cookie, so it cannot be replayed by someone reading these logs.

### Slow and failed requests, as the app saw them

Use this when: you want the app's own opinion, including requests that failed before ingress could
tell you why.

```kusto
ContainerAppConsoleLogs
| where TimeGenerated > ago(24h) and ContainerAppName == "zmg-app"
| where Log startswith "{"
| extend p = parse_json(Log)
| where toint(p.EventId) in (3000, 3001, 3002, 3003)
| project TimeGenerated,
          Event         = case(toint(p.EventId) == 3000, "slow",
                               toint(p.EventId) == 3001, "failed",
                               toint(p.EventId) == 3002, "unhandled",
                                                         "rejected"),
          Method        = tostring(p.State.Method),
          Path          = tostring(p.State.Path),
          Status        = toint(p.State.StatusCode),
          Ms            = tolong(p.State.ElapsedMs),
          CorrelationId = extract(@'"CorrelationId":"([^"]+)"', 1, Log),
          Exception     = tostring(p.Exception)
| order by TimeGenerated desc
```

Nothing appears here for a fast, successful request — by design. The threshold for `slow` is
`Logging:SlowRequestMs`, 1000 ms by default. A signed-out `GET /api/auth/me` 401 is excluded too: it
is the SPA's probe answering "signed out", and it would otherwise be the most common line in the file.

### Slowest endpoints

Use this when: the app feels sluggish and you need to know where.

```kusto
ContainerAppHTTPLogs
| where TimeGenerated > ago(24h) and ContainerAppName == "zmg-app"
| where Path startswith "/api/"
// Collapse ids so /api/releases/<guid> doesn't fragment into one row per release.
| extend Route = replace_regex(Path, @"/[0-9a-fA-F]{8}-[0-9a-fA-F-]{27}", "/{id}")
| summarize Count = count(),
            p50   = percentile(RequestDuration, 50),
            p95   = percentile(RequestDuration, 95),
            Max   = max(RequestDuration)
        by Method, Route
| order by p95 desc
```

`RequestDuration` is **milliseconds**. Expect a handful of multi-second outliers with no cause in the
app: those are cold starts, not slow code — see below.

### Is this deploy worse than the last one?

Use this when: you just shipped, and you are deciding whether to roll back.

```kusto
ContainerAppHTTPLogs
| where TimeGenerated > ago(6h) and ContainerAppName == "zmg-app"
| summarize Requests = count(),
            Failures = countif(StatusCode >= 500),
            p95      = percentile(RequestDuration, 95),
            First    = min(TimeGenerated)
        by RevisionName
| extend FailureRate = round(100.0 * Failures / Requests, 2)
| order by First desc
```

Rollback is `deploy.yml`'s `workflow_dispatch` against an earlier image tag — but read the
"rolling back the image does not roll back the schema" rule in
[`infra/README.md`](../infra/README.md) first.

### Cold starts

Use this when: someone says "it was slow the first time". Two different numbers, and confusing them is
the mistake M40/M41 were about.

**What the app took to boot** — reliably ~0.15s, and *not* what the user waited for:

```kusto
ContainerAppConsoleLogs
| where TimeGenerated > ago(7d) and ContainerAppName == "zmg-app"
| where Log has "[boot]" and Log has "Application started"
| extend Ms = toint(extract(@"(\d+) ms", 1, Log))
| project TimeGenerated, RevisionName, ContainerGroupName, Ms
| order by TimeGenerated desc
```

**What the user actually waited for** — the first request after a scale-from-zero, dominated by ~11–12s
of Azure sandbox provisioning that no code change touches:

```kusto
ContainerAppHTTPLogs
| where TimeGenerated > ago(7d) and ContainerAppName == "zmg-app"
| where RequestDuration > 5000
| project TimeGenerated, Method, Path, StatusCode, RequestDuration, RevisionName
| order by TimeGenerated desc
```

The gap between the two queries is the entire point of M40–M42: boot got 13× faster and users felt
nothing, because the wait was never the app. Don't re-propose ReadyToRun.

### Cover uploads

Use this when: someone says an upload failed, or you want to check the encoder is still doing its job.

```kusto
ContainerAppConsoleLogs
| where TimeGenerated > ago(30d) and ContainerAppName == "zmg-app"
| where Log startswith "{"
| extend p = parse_json(Log)
| where toint(p.EventId) == 2000
| extend SourceBytes = tolong(p.State.SourceBytes), StoredBytes = tolong(p.State.StoredBytes)
| project TimeGenerated,
          Outcome = tostring(p.State.Outcome),
          SourceBytes,
          StoredBytes,
          Ratio   = round(1.0 * StoredBytes / SourceBytes, 2),
          Ms      = tolong(p.State.ElapsedMs)
| order by TimeGenerated desc
```

**Watch `Ratio`.** A photo normalized to a 1000px WebP should land far below 1.0. A ratio near or above
it is the M33 failure returning — the encoder silently emitting *lossless* WebP, where `Quality` is
ignored and a 4.3 MB source came back at 2.9 MB with every unit test green. That shape is only visible
here.

### Blocked remote fetches

Use this when: you want to know whether anyone is walking the SSRF guard.

```kusto
ContainerAppConsoleLogs
| where TimeGenerated > ago(30d) and ContainerAppName == "zmg-app"
| where Log startswith "{"
| extend p = parse_json(Log)
| where toint(p.EventId) in (2001, 2002)
| project TimeGenerated,
          Event  = iff(toint(p.EventId) == 2001, "blocked", "unreachable"),
          Reason = tostring(p.State.Reason),
          Host   = tostring(p.State.Host)
| order by TimeGenerated desc
```

`blocked_address` on the first hop is usually a bad paste. **`blocked_redirect_address` is not** — it
means a host that resolved publicly then redirected at an internal address, which is the exact attack
the manual redirect-following exists to stop.

### Traffic that bypassed the Worker

Use this when: you want to see who is hitting the ACA origin directly. The FQDN is publicly reachable
and always will be — it is the rollback target — so this is worth an occasional look.

```kusto
ContainerAppHTTPLogs
| where TimeGenerated > ago(24h) and ContainerAppName == "zmg-app"
| extend ClientIp    = tostring(split(XForwardedFor, ",")[0]),
         ViaCloudflare = XForwardedFor contains ","
| summarize Requests = count(), Paths = make_set(Path, 10), Agents = make_set(UserAgent, 5)
        by ClientIp, ViaCloudflare
| order by Requests desc
```

Browser traffic arrives through Cloudflare and carries **two** entries in `XForwardedFor` — the real
client first, Cloudflare's edge second. A single entry means something reached ACA directly. Note
`Authority` cannot tell you this: the Worker rewrites `Host` to the ACA FQDN because the ingress routes
on it, so every row looks the same there.

### Am I still free?

Use this when: monthly, or after turning on a new log category. The allowance is 5 GB/month.

```kusto
Usage
| where TimeGenerated > ago(30d) and IsBillable == true
| summarize GB = round(sum(Quantity) / 1000, 3) by DataType
| order by GB desc
```

`ContainerAppHTTPLogs` grows with traffic and `ContainerAppConsoleLogs` does not, so the HTTP table is
the one to watch if this app ever gets busy.

### Querying history before the switch

Use this when: you need something from before 2026-07-28.

```kusto
ContainerAppConsoleLogs_CL
| where TimeGenerated between (datetime(2026-07-01) .. datetime(2026-07-28))
| where ContainerAppName_s == "zmg-app"
| project TimeGenerated, RevisionName_s, Log_s
| order by TimeGenerated desc
```

Those rows are **plain text, not JSON** — pre-M57 the app used the readable console formatter, which
also split one logical entry across two lines (`info: Zmg.Api[0]`, then the indented message). There is
no `ContainerAppHTTPLogs` history at all; ingress logging began with the M58 switch.

---

## Gotchas

Each of these makes a query return the wrong answer *without erroring*, which is the only kind worth
writing down.

### `CorrelationId` is not `RequestId`

An app log line's `Scopes` array contains **both**, and they are different values:

| Key | Value | Joins to |
|---|---|---|
| `CorrelationId` | ACA/Envoy's `x-request-id` | `ContainerAppHTTPLogs.RequestId` ✅ |
| `RequestId` | Kestrel's per-connection id (`0HNNCKU487K0D`) | nothing outside the process ❌ |

ASP.NET's hosting scope publishes that second one and there is no way to stop it, which is precisely
why M57 named ours `CorrelationId` rather than letting two values share a key. **Always join on
`CorrelationId`.**

### The scopes array is the fragile part

`Scopes` is a heterogeneous array whose contents depend on what the framework pushed, so
`p.Scopes[2].CorrelationId` is right until the day it silently isn't. Two safe options:

```kusto
// Simple, position-independent, keeps rows that have no scopes at all (boot lines).
| extend CorrelationId = extract(@'"CorrelationId":"([^"]+)"', 1, Log)
```

```kusto
// Structured, but note it DROPS every row whose scopes don't match — including boot lines.
| mv-apply s = p.Scopes on (
    where isnotempty(s.CorrelationId) | project CorrelationId = tostring(s.CorrelationId))
```

The recipes above use `extract` for that reason: silently losing rows is worse than a regex.

### Filter on `EventId`, never on message text

Event ids are permanent identifiers — the same rule the API's message codes and integer enums carry.
Message wording is not; rewording one is expected to break nothing, and a query matching on it is the
thing that breaks.

| Range | Events |
|---|---|
| 1000–1003 | `auth.login.ok`, `auth.login.denied`, `auth.logout`, `auth.session.swept` |
| 2000–2002 | `cover.upload`, `cover.fetch.blocked`, `cover.fetch.failed` |
| 3000–3003 | `request.slow`, `request.failed`, `request.unhandled`, `request.rejected` |

`EventId: 0` means no id was assigned — the `[boot]` lines and anything from a framework category.

### Old revisions logged plain text

`parse_json(Log)` returns null for every row written before M57 deployed, and for anything the
framework writes outside our formatter. The `| where Log startswith "{"` guard in every recipe is what
keeps those rows from quietly becoming nulls in your results.

### `Path` never has a query string

Deliberate, and load-bearing: ACA's own HTTP log documentation warns that a path can carry secrets when
clients put them in the query. Ours don't, and logging without the query is what keeps that true. The
denied-login redirect carries no email for the same reason. Don't go looking for parameters — and don't
add them.

### `RequestDuration` is milliseconds

Not microseconds. Confirmed against a known cold start: 24,698 for a request measured at ~24.7s.

### Nothing else logs who did what

Auth events carry an email. Business writes — creating a release, editing a checklist, deleting an
artist — record **nothing** about the actor, by decision. If you need "who changed this", it does not
exist in these logs, and adding it is a design change, not a query.
