# Custom domain migration — `app.zionmusicgroup.com`

> **Status:** not started. Written 2026-07-26 as a reference to execute later.
> **This file is intentionally untracked.** It is self-contained — it can be moved out of the repo
> and still make sense. Nothing else in the repo was changed to create it.

## Goal

Replace `https://zmg-tracker.zmg-app.workers.dev` with `https://app.zionmusicgroup.com` as the
front door for the ZMG Release Tracker SPA, **without changing anything about how
`zionmusicgroup.com` itself behaves**, and **at zero additional recurring cost**.

Non-goals: touching the Azure Container App, changing the API surface, changing how the Netlify
marketing site is built or deployed, or transferring the domain registrar.

---

## 1. Current state (verified 2026-07-26, **re-verified 2026-07-27**)

Everything below was read from the authoritative nameserver (`dns1.p04.nsone.net`) and the .com
registry, not from a local resolver. Re-verify before executing — this snapshot will age.

> ✅ **Re-verified 2026-07-27**: NS, SOA, apex A, `www` A, MX and the absence of TXT/CAA/DS all still
> match. Nothing in the zone has changed.

> 🛑 **Your local resolver lies about MX. Pin a resolver on every check below.**
> The LAN router (`fe80::1`) synthesizes MX answers for this domain: 8 of 10 consecutive
> `dig MX zionmusicgroup.com` queries returned `0 ipnz74i5v1rm8q65.dom.` /
> `10 mx1.ipnz74i5v1rm8q65.dom.` instead of `1 smtp.google.com.`. `A` and `NS` are **not** affected —
> 10/10 and 5/5 correct — so the interference is MX-specific.
>
> This is a pre-existing local-network fault, unrelated to this migration, and it does **not** affect
> real mail: sending servers resolve MX through their own resolvers, and Cloudflare, Google and
> Quad9 all agree on `1 smtp.google.com.`
>
> It matters here for one reason: **MX is the highest-risk record in this plan and the one whose
> failure you would not otherwise notice.** Unpinned, the §5 Phase 3 gate would report broken mail
> ~80% of the time immediately after cutover — and on the 20% it happened to pass, it would be
> passing by luck rather than by evidence. Every command in §10 now pins `@1.1.1.1`. Worth fixing the
> router separately; an MX-rewriting resolver on your LAN is broken at best.

**Registrar:** Namecheap. **DNS:** Netlify DNS (NS1-powered). **Site host:** Netlify.

```
zionmusicgroup.com.  172800  NS   dns1.p04.nsone.net.   (also dns2, dns3, dns4)
zionmusicgroup.com.  3600    SOA  dns1.p01.nsone.net. domains+netlify.netlify.com. …
zionmusicgroup.com.  120     A    98.84.224.111
zionmusicgroup.com.  120     A    18.208.88.157
www.zionmusicgroup.com. 120  A    98.84.224.111
www.zionmusicgroup.com. 120  A    18.208.88.157
zionmusicgroup.com.          MX   1 smtp.google.com.
```

`curl -I https://zionmusicgroup.com` → `server: Netlify`.

**Findings that shape the plan:**

- **Namecheap is only the registrar.** The zone lives in the *Netlify* dashboard
  (Domains → `zionmusicgroup.com`). Namecheap's "Advanced DNS" tab is inert. The only thing you
  change at Namecheap is the nameserver field.
- **Mail is Google Workspace** (`MX 1 smtp.google.com`). This record is business-critical. Losing
  it during migration silently breaks inbound email. It is the single highest-risk item here.
- **DNSSEC is NOT enabled** — no DS record at the .com registry. This is good news: a nameserver
  change with DNSSEC on takes the domain completely dark until the DS is fixed. Confirm this is
  still true immediately before cutover.
- **No TXT records at all** — no SPF, no DKIM, no DMARC, no CAA. See §9, out of scope but worth
  knowing.
- **Registry NS TTL is 172800 (48h).** Rollback by reverting nameservers is *slow*. This is why §7
  says do not delete the Netlify DNS zone.
- **The zone is small** — apex, `www`, and one MX. That makes this a low-risk migration.

> ✅ **Zone enumerated from the Netlify DNS panel, 2026-07-27.** The guessing is over — the zone holds
> **exactly three records**, and there is nothing I failed to discover:
>
> | Name | TTL | Type | Value |
> |---|---|---|---|
> | `zionmusicgroup.com` | 1 | **MX** | `1 smtp.google.com` |
> | `zionmusicgroup.com` | 3600 | **NETLIFY** | `rad-tulumba-de1747.netlify.app` |
> | `www.zionmusicgroup.com` | 3600 | **NETLIFY** | `rad-tulumba-de1747.netlify.app` |
>
> **Netlify site name: `rad-tulumba-de1747.netlify.app`.** No TXT, no CAA, no DKIM, no
> domain-verification records — which confirms §9's finding as complete rather than merely
> unobserved.

> 🛑 **The apex and `www` are `NETLIFY` records, not `A` records — do not trust Cloudflare's scan.**
> `NETLIFY` is a proprietary ALIAS-style pseudo-record that exists only inside Netlify DNS. The
> `98.84.224.111` / `18.208.88.157` that `dig` returns are what it *resolves to*, not what is stored.
>
> Cloudflare's onboarding scan works by resolving names, so it will import those two IPs as literal
> `A` records. They would work — but they are the addresses Netlify hands out for **Netlify-managed**
> zones, not the address Netlify documents and supports for **external** DNS. Accepting the scan
> means silently depending on IPs nobody promised to keep stable for you.
>
> This is exactly why §5 specifies `75.2.60.5` at the apex. It was a judgment call when written; the
> record type confirms it as a requirement. **Reconcile every scanned record by hand against the §5
> target table.**

---

## 2. Why the whole domain has to move to Cloudflare

The obvious shortcut does not work:

```
app.zionmusicgroup.com.  CNAME  zmg-tracker.zmg-app.workers.dev.   ← does NOT work
```

Cloudflare refuses to serve a Worker for a `Host` header belonging to a zone it doesn't control.
You get **`Error 1014: CNAME Cross-User Banned`**. A Workers Custom Domain requires the zone to be
on Cloudflare.

The two ways to attach only a subdomain are both paywalled:

| Approach | Availability | Verdict |
|---|---|---|
| **Subdomain zone** — add `app.zionmusicgroup.com` as its own zone | Enterprise only | ✗ |
| **Partial / CNAME setup** — keep DNS elsewhere, delegate one hostname | Business plan or above | ✗ |
| **Full setup** — move `zionmusicgroup.com`'s nameservers to Cloudflare | **Free plan** | ✓ |

So: full nameserver move, Free plan. The Netlify site keeps serving the marketing pages exactly as
it does now — Netlify just stops being the DNS provider. That is the entire cost of this change.

**Rejected alternative:** point `app.zionmusicgroup.com` straight at the Azure Container App (ACA
supports custom domains + free managed certs via CNAME + an `asuid` TXT). This works with Netlify
DNS untouched, but it bypasses the Worker entirely and brings back the ~17–22s cold-start blank
page — undoing M40/M41. Only worth reconsidering if the DNS move becomes impossible.

---

## 3. Cost: stays at $0

| Item | Tier | Cost | Headroom |
|---|---|---|---|
| Cloudflare zone / DNS | Free | **$0** | Unlimited records, unlimited queries |
| Universal SSL for `app.zionmusicgroup.com` | Free | **$0** | Covers apex + **one** subdomain level |
| Workers (already in use) | Free | **$0** | 100k requests/day |
| Workers **Custom Domain** | Free | **$0** | — |
| Static assets served from the edge | Free | **$0** | Asset requests aren't billed as Worker invocations |
| Namecheap nameserver change | — | **$0** | Registrar and renewal price unchanged |
| Netlify site | existing free tier | **$0** | Moving DNS off Netlify DNS doesn't change the site's plan |
| Azure Container App | unchanged | unchanged | Not touched by this plan |
| R2 custom domain (§8, optional) | Free | **$0** | 10 GB storage, zero egress fees |

### Things that would break $0 — deliberately avoided

- **Advanced Certificate Manager — $10/mo.** Only needed for *multi-level* subdomains. Universal
  SSL covers `app.zionmusicgroup.com` but would **not** cover `app.zmg.zionmusicgroup.com`.
  → **Use a single label. Never nest.** This is the easiest way to accidentally incur a bill.
- **Total TLS** — requires ACM. Not needed.
- **Workers Paid — $5/mo.** Only past 100k req/day. Nowhere near it.
- **Argo Smart Routing / Load Balancing / paid WAF / Images** — all off by default. Leave them off.
- **Cloudflare Registrar transfer** — genuinely optional and *not* part of this plan. Cloudflare
  sells .com at wholesale (often below Namecheap's renewal), so it could be cost-*negative*, but
  it's a separate decision with its own transfer window and auth-code dance. Keep it out of scope.

### ⚠️ The one free feature that will hurt you

**Do NOT enable Cloudflare Email Routing.** It is free and Cloudflare will offer it prominently
during onboarding. Enabling it **overwrites the zone's MX records** with Cloudflare's own — which
would break Google Workspace mail for the domain. Decline it. If you click it by accident, restore
`MX 1 smtp.google.com` immediately.

---

## 4. Pre-flight checklist

Do these before touching anything:

- [ ] **Log into Netlify and screenshot the complete DNS record list for the zone.** This is your
      source of truth and your rollback reference. *(Blocks Phase 1 — my probe cannot enumerate a
      zone, so the panel is the only complete list.)*
- [ ] **Note the Netlify site name** — you need `<site-name>.netlify.app` for the `www` record.
      Find it under Site configuration → Site details. *(Blocks Phase 1.)*
- [x] ~~Re-run the verification in §10 and confirm reality still matches §1.~~ **Done 2026-07-27** —
      matches, plus the resolver finding in §1.
- [x] ~~Confirm DNSSEC is still off.~~ **Done 2026-07-27** —
      `dig +short DS zionmusicgroup.com @a.gtld-servers.net` returned empty. Re-check immediately
      before cutover; if it ever returns anything, **stop** and disable DNSSEC first, then wait out
      the DS TTL.
- [x] ~~Confirm the live state is healthy before changing anything.~~ **Done 2026-07-27** — apex
      `200` via `Netlify Edge`; `www` `301 → https://zionmusicgroup.com/` (`server: Netlify`);
      `zmg-tracker.zmg-app.workers.dev` `200` with `cf-cache-status: HIT`; `/api/health` →
      `{"status":"ok"}`.

> **`www` redirects, and that is a Netlify *site* setting, not DNS.** It currently answers `301` to
> the apex rather than serving content. Moving DNS does not carry that redirect — it keeps working
> because the site keeps its domain aliases and primary-domain config. So the Phase 3 gate for `www`
> is "still 301s to the apex", not "serves the site".
- [ ] If Netlify DNS lets you set TTLs, drop them to 300s a day ahead. Doesn't speed up the NS
      delegation (that's a fixed 48h registry TTL), but it de-risks intermediate fixes.
- [ ] Pick a low-traffic window. Cutover itself is instant; propagation is the slow part.
- [ ] Confirm you can receive a test email at the domain *before* you start, so a post-cutover
      failure is unambiguous.

---

## 5. Migration steps

### Phase 1 — Build the Cloudflare zone *before* flipping anything

1. Add `zionmusicgroup.com` to Cloudflare, **Free plan**. Let the automatic scan run.
2. Reconcile the scanned records against your Netlify screenshot. The scan is a convenience, not a
   guarantee — it can miss records. Add anything missing by hand.
3. Target state:

| Type | Name | Value | Proxy | Notes |
|---|---|---|---|---|
| A | `zionmusicgroup.com` | `75.2.60.5` | **DNS only** (grey) | Netlify's documented external-DNS apex IP. **Not** the scanned `98.84.224.111` / `18.208.88.157`. |
| CNAME | `www` | `rad-tulumba-de1747.netlify.app` | **DNS only** (grey) | Confirmed site name, 2026-07-27 |
| MX | `zionmusicgroup.com` | `smtp.google.com` priority `1` | n/a | **Critical.** Verify character-for-character. |

That is the **entire** zone — three records in, three records out. Anything else the scan proposes is
either a resolved artefact of the `NETLIFY` pseudo-records (delete it) or something Cloudflare
invented (Email Routing — decline it).

   Notes on that table:

   - **Grey-cloud everything that exists today.** DNS-only means Cloudflare answers the query and
     gets out of the way — Netlify serves the site and issues its own certificate, exactly as now.
     This is what keeps the marketing site's behaviour bit-identical. Orange-clouding Netlify would
     add a proxy hop, a second certificate layer, and a redirect-loop risk if the SSL/TLS mode isn't
     Full (strict). No upside here. Don't.
   - **Use `75.2.60.5` at the apex, not the current `98.84.224.111` / `18.208.88.157`.** Those two
     are what Netlify DNS hands out for a Netlify-managed zone; `75.2.60.5` is the value Netlify
     documents for *external* DNS providers, and it's the stable one. Both serve the same site, so
     the split-brain window in Phase 2 is harmless either way.
   - **Do not use a CNAME at the apex.** Cloudflare's CNAME flattening does technically allow it to
     coexist with MX, but with Google Workspace mail on the line there is no reason to introduce
     that subtlety. A record. Done.
   - Add a **CAA** record only if you deliberately want one. There is none today; adding a wrong
     one blocks certificate issuance for both Netlify and Cloudflare.

4. Cloudflare shows you two assigned nameservers. Write them down.
5. **Verify the Cloudflare zone answers correctly before cutover**, by querying it directly:

```bash
dig +noall +answer @<your-cloudflare-ns> zionmusicgroup.com A MX; dig +noall +answer @<your-cloudflare-ns> www.zionmusicgroup.com
```

   Both nameserver sets now return working answers. The cutover is a no-op from a resolver's
   perspective, which is the whole point.

> ✅ **Phase 1 executed and verified 2026-07-27.**
>
> **Assigned Cloudflare nameservers:** `grant.ns.cloudflare.com`, `shaz.ns.cloudflare.com`
>
> Records entered **manually** (quick scan deliberately skipped — it would have imported the resolved
> `NETLIFY` IPs as literal A records). Verified identical on *both* nameservers, TTL 300:
>
> ```
> zionmusicgroup.com.      300 IN A     75.2.60.5
> zionmusicgroup.com.      300 IN MX    1 smtp.google.com.
> www.zionmusicgroup.com.  300 IN CNAME rad-tulumba-de1747.netlify.app.
> ```
>
> **Beyond DNS — the targets were proven to serve correctly before cutover**, by forcing connections
> with `curl --resolve`:
>
> - Apex forced to `75.2.60.5` → `HTTP/2 200`, `cache-status: "Netlify Edge"; hit`.
> - TLS on `75.2.60.5` with SNI `zionmusicgroup.com` → **valid Let's Encrypt cert already present**,
>   `CN=zionmusicgroup.com`, valid 2026-07-25 → 2026-10-23. **Cutover triggers no cert re-issue and
>   no validation window** — the certificate is live on the target IP today.
> - `www` target `rad-tulumba-de1747.netlify.app` → `301 → https://zionmusicgroup.com/`, matching
>   current behaviour exactly.
>
> This is the difference between "DNS answers" and "the answer is correct". Both now hold.

### Phase 2 — Cut over at Namecheap

6. Namecheap → Domain List → `zionmusicgroup.com` → Manage → **Nameservers** → **Custom DNS** →
   enter Cloudflare's two nameservers → save.
7. Wait. Cloudflare emails when it detects the change (usually minutes; the registry NS TTL means
   stragglers can take up to 48h). During this window resolvers hit either NS1 or Cloudflare and
   both are correct — that's why Phase 1 comes first.

> ✅ **Phase 2 executed 2026-07-27.** Nameservers changed at Namecheap from the four `p04.nsone.net`
> entries to `grant.ns.cloudflare.com` + `shaz.ns.cloudflare.com`. Registry delegation had already
> propagated when checked minutes later — no 48h wait materialised in practice, though the TTL means
> individual resolvers still may.
>
> **Phase 3 gate, all passing:**
>
> | Check | Result |
> |---|---|
> | `.com` registry delegation | `grant` + `shaz`.ns.cloudflare.com |
> | DS at registry | empty — DNSSEC never enabled |
> | MX via 1.1.1.1 / 8.8.8.8 / 9.9.9.9 | `1 smtp.google.com.` on all three |
> | Apex A | `75.2.60.5` |
> | `www` | CNAME → `rad-tulumba-de1747.netlify.app` → Netlify |
> | Apex HTTP | `HTTP/2 200`, `server: Netlify` |
> | `www` HTTP | `301 → https://zionmusicgroup.com/` |
>
> A test email to the domain was confirmed delivered **before** cutover, per §4, so a post-cutover
> failure would be unambiguous.

### Phase 3 — Verify the marketing site is untouched

This is the acceptance gate. Do not proceed to Phase 4 until all of these pass:

- [ ] `https://zionmusicgroup.com` loads, with valid TLS, identical content.
- [ ] `https://www.zionmusicgroup.com` loads (or redirects as it did before).
- [ ] `curl -sSI https://zionmusicgroup.com | grep -i server` still says `Netlify`.
- [ ] `dig +short MX zionmusicgroup.com @1.1.1.1` → `1 smtp.google.com.`
      **Pin the resolver** — see the §1 warning. Unpinned this check is ~80% false alarms on this
      machine. Cross-check with `@8.8.8.8` before believing any failure.
- [ ] **Send a real test email to an address at the domain and confirm delivery.** DNS looking
      right is not the same as mail working.
- [ ] Netlify's dashboard doesn't flag a domain configuration error, and its certificate is valid
      (it re-issues via HTTP validation once DNS points at it externally).

### Phase 4 — Clean up Netlify DNS (only after a week)

8. Once you're confident — **wait at least 7 days** — remove the now-dormant DNS zone from Netlify
   (Domains → zone → delete). Keep the domain *attached to the site* so it keeps answering on that
   `Host` and renewing its certificate. Deleting the zone earlier destroys your rollback path (§7).

### Phase 5 — Attach the Worker custom domain

9. Cloudflare → Workers & Pages → `zmg-tracker` → Settings → **Domains & Routes** → Add → Custom
   Domain → `app.zionmusicgroup.com`.
10. Cloudflare creates the proxied DNS record and issues the certificate itself. Give it a few
    minutes.
11. Verify:

```bash
curl -sSI https://app.zionmusicgroup.com | head -5 && curl -sS https://app.zionmusicgroup.com/api/health
```

    Expect a 200 with the SPA shell, and `{"status":"ok"}` from the health endpoint proxied through
    to ACA (allow for a cold start on the first hit).

12. Click through the app: a deep link like `https://app.zionmusicgroup.com/catalog/<id>` must
    return `index.html`, not a 404 — that's `not_found_handling: "single-page-application"` doing
    its job. Confirm a cover image loads and a write operation succeeds.

> ✅ **Phase 5 executed and verified 2026-07-27.**
>
> | Check | Result |
> |---|---|
> | DNS | `172.67.150.101`, `104.21.71.227` — proxied (orange), as Cloudflare created it |
> | TLS SAN | `zionmusicgroup.com`, `*.zionmusicgroup.com`, Google Trust Services, valid to 2026-10-25 |
> | SPA shell | `HTTP/2 200`, `cf-cache-status: HIT`, `server: cloudflare` |
> | Deep link | `/catalog/<junk>` → `200` + `<div id="root">` (SPA fallback, not 404) |
> | `/api/health` | `{"status":"ok"}` in 0.44s warm |
> | DB via new origin | 2 templates, Single **31** / Album **41** tasks, `Mix/master` / `Mezcla/master` |
>
> **The wildcard is single-level — confirmed, not assumed.** The Universal SSL SAN is
> `*.zionmusicgroup.com`, which covers `app.` and would **not** cover `app.zmg.zionmusicgroup.com`.
> §3's "use a single label, never nest" rule is now evidenced by the certificate itself.
>
> **Attaching hit one snag worth recording.** The Worker's *Connect domain* picker first reported
> *"No zones match app.zionmusicgroup.com"* even though the zone was live and the registry already
> delegated to Cloudflare. Cause: the zone had not yet flipped to **Active** in Cloudflare's own
> view — the picker lists Active zones only. It resolved itself minutes later with no intervention.
> Diagnostic that ruled out the scarier explanation (zone in the wrong Cloudflare account): compare
> the account id in the dashboard URL against `r2_account_id` in `infra/terraform.tfvars` — they
> matched (`aa697a66…`), so no zone move was needed.
>
> **Standing nag to keep declining:** the zone Overview shows an amber *"Proxy DNS records"* badge
> urging you to orange-cloud the grey records. Never accept it for the Netlify apex/`www` — that is
> the proxy-hop/redirect-loop risk in §5. `app` is the only record that should be orange, and
> Cloudflare set that one itself.

---

## 6. Repo changes

The application needs essentially nothing, which is the point of the current architecture:

- `API_ORIGIN` in `wrangler.jsonc` is **unchanged** — the Worker still proxies to the ACA FQDN.
- `src/api/client.ts` is **unchanged** — it calls same-origin `/api/*` paths.
- **No production CORS policy is needed.** `Program.cs` scopes CORS to Development only, and
  `/api/*` still lives on the same hostname as the SPA. Serving the SPA from a genuinely separate
  origin *would* have required all three of these to change; a custom domain on the Worker does not.

### 6a. `src/Zmg.Web/wrangler.jsonc`

Add the custom domain to config so it's reproducible rather than click-ops:

```jsonc
"routes": [
  { "pattern": "app.zionmusicgroup.com", "custom_domain": true }
]
```

Optionally, once the custom domain is proven, retire the workers.dev hostname:

```jsonc
"workers_dev": false
```

Keep `workers.dev` enabled through at least the first week — it's a free second path to the same
deployment while you're still validating. The ACA URL remains the real fallback and rollback target
either way; the container must keep building and serving the SPA from `wwwroot`.

### 6b. Widen `CLOUDFLARE_API_TOKEN` (the CI token)

You've said you're fine with this, and it does make future changes easier — DNS and routes become
part of the same deploy rather than a dashboard errand.

Current token: Account → **Workers Scripts: Edit**. Add, **scoped to the `zionmusicgroup.com` zone
only** (not "all zones"):

- Zone → **Workers Routes: Edit**
- Zone → **DNS: Edit**
- Zone → **Zone: Read**

Then update the GitHub Actions secret. Verify with a manual `wrangler deploy` from your machine
before relying on CI.

> Note what you're accepting: this token now lives in GitHub Actions and can change DNS for the
> domain that serves your marketing site and routes your email. The mitigation is scoping it to the
> one zone and keeping it out of `terraform.tfvars`. Consider it a deliberate trade, not a
> non-event — `infra/README.md` documents the opposite choice for the Terraform token and the
> reasoning there is still sound.

### 6c. Docs to update (deliberately not touched by this plan)

- `README.md:140` — the URL table; `app.zionmusicgroup.com` becomes the primary row.
- `infra/README.md:92` — the "Edge SPA" section names the workers.dev URL.
- `infra/README.md` "Deliberately not managed by Terraform" — revise the Cloudflare-token paragraph
  if §6b and/or §7 land, since it argues for the narrow token.
- `plans/PROGRESS.md` — journal entry + backlog adjustment, per the build workflow.

---

## 7. Optional: adopt the zone into Terraform

Consistent with `infra/`'s existing pattern (create by hand, then `import` — see `imports.tf`),
because zone onboarding needs the interactive scan and the registrar step regardless.

**Decision 2026-07-27: Terraform owns the Worker custom domain, and nothing else Cloudflare-side.**
`wrangler.jsonc` gets **no** `routes` block, so §6a and §6b are **dropped** — the CI token stays
`Workers Scripts: Edit` only and never gains DNS rights.

### Schema, verified against the provider (not guessed)

The warning below this section originally said the resource names were unconfirmed. They are now
confirmed against `cloudflare/terraform-provider-cloudflare` `main`, which matches the pinned
`~> 5.12`:

| Need | v5 resource | Notes |
|---|---|---|
| Worker custom domain | `cloudflare_workers_custom_domain` | required `hostname`, `service`; optional `account_id`, `zone_id`, `zone_name` |
| DNS record (if ever needed) | `cloudflare_dns_record` | renamed from v4's `cloudflare_record` |
| Zone (if ever needed) | `cloudflare_zone` | `account = { id = … }` nested attribute in v5 |

> 🔑 **The permission estimate above was wrong, in your favour.**
> `cloudflare_workers_custom_domain` declares its accepted permissions as **`Workers Scripts Read`**
> and **`Workers Scripts Write`** — and nothing else. It does **not** need `DNS: Edit`,
> `Zone: Read`, or `Workers Routes: Edit`.
>
> That is because Cloudflare creates the proxied `app` DNS record itself as a side effect of
> attaching the custom domain. Terraform describes the *binding*; Cloudflare owns the record. So the
> Terraform token never gains the ability to rewrite DNS for the domain that routes company email —
> which was the whole risk this section was trying to price.

### Scope: the binding only

**Do not** put the zone or the three DNS records under Terraform. Reasons:

- It would require `Zone: Write` + `DNS: Write` on the token, reintroducing exactly the risk the
  narrow scope avoids, to manage three records that are static and written down verbatim in §1.
- A replaced DNS record is a brief outage; **a replaced MX record is lost mail.** There is no version
  of that trade worth taking for reproducibility of three lines.
- The zone itself needs the interactive onboarding + registrar step regardless, so codifying it buys
  nothing.

### Honest cost

This adds `Workers Scripts: Write` to `var.cloudflare_api_token`, which `infra/README.md` currently
records as *deliberately avoided* — it is the stated reason the Worker is hand-created rather than
Terraform-managed. That note needs updating, and the trade should be seen rather than skipped:

- **Against:** the Terraform token can now modify Worker scripts, not just R2.
- **For:** it lives only in gitignored `terraform.tfvars`, never in GitHub Actions, and
  `Workers Scripts: Write` cannot touch DNS, mail, or the zone. Compare §6b, which would have put a
  DNS-editing token *in CI*.

### Steps

1. Cloudflare → My Profile → API Tokens → edit the Terraform token → add **Account · Workers
   Scripts · Edit**. Leave R2 as-is. Add **no** zone permissions.
2. Find the custom domain's immutable id (the import id is `<account_id>/<domain_id>`):

```bash
curl -s "https://api.cloudflare.com/client/v4/accounts/aa697a66b815f21d509abc14613b070d/workers/domains" \
  -H "Authorization: Bearer $CF_TOKEN" | python3 -m json.tool
```

   That one call returns **both** ids this needs: the domain's `id` and its `zone_id`.

3. Append to `infra/cloudflare.tf`:

```hcl
# Binds app.zionmusicgroup.com to the zmg-tracker Worker (v2.10/M53). Cloudflare creates the
# proxied DNS record and issues the certificate itself, so this resource needs only Workers
# Scripts permissions — the token holds no DNS rights, deliberately. The zone and its three
# records stay hand-managed; see §7 of the migration doc for why.
resource "cloudflare_workers_custom_domain" "app" {
  account_id = var.r2_account_id
  hostname   = "app.zionmusicgroup.com"
  service    = "zmg-tracker"
  zone_id    = "d9fa1a74f7901d35ada4efad42497d67"
  zone_name  = "zionmusicgroup.com"
}
```

   Declare **both** `zone_id` and `zone_name`, matching the provider's own example. An optional
   attribute left out of config but present in state is a common source of a permanently non-empty
   plan after an import. The zone id is inlined rather than made a variable: it is a public
   identifier used exactly once, and `imports.tf` already hardcodes subscription ids the same way.

   > **`environment` is deliberately omitted.** The live resource has `environment: "production"`,
   > but the provider marks that attribute **deprecated**. Start without it. If `terraform plan`
   > shows *any* diff on `environment`, **do not apply** — add `environment = "production"` and
   > re-plan. A change to `environment` can force replacement, and replacing this resource detaches
   > the domain and re-issues its certificate.

4. Add to `infra/imports.tf`:

```hcl
import {
  to = cloudflare_workers_custom_domain.app
  id = "aa697a66b815f21d509abc14613b070d/70c11747a896bc31aa0221f410b6a624dd3050a6"
}
```

   **Keep the block after applying.** Every other adoption in `imports.tf` is still there —
   `import` blocks are no-ops once the resource is in state, and leaving them is what documents
   that this stack was imported rather than created. That is the file's actual convention.

5. `az login`, then `terraform plan` — **iterate until it is empty.** Per `infra/README.md`: a
   `forces replacement` means the config is wrong, so fix the config, not reality. Replacing this
   resource would briefly detach the domain and re-issue its certificate.

   > **No `terraform init` needed, and never `-upgrade` here.** Adding a new *resource type* from an
   > already-installed provider requires no re-initialization — `.terraform/` already holds the
   > provider binary. `-upgrade` would re-resolve `~> 5.12` against the registry, likely bump the
   > pinned Cloudflare **5.22.0**, and rewrite `.terraform.lock.hcl`, which is **tracked in git**.
   > An unrequested provider upgrade during an import is precisely how a plan stops being empty for
   > reasons unrelated to your change. Run plain `terraform init` only if `plan` complains the
   > backend or providers aren't initialized.
   >
   > The `cloudflare_workers_custom_domain` schema was verified against the **v5.22.0 tag**, not
   > just `main` — identical.

6. `terraform apply` once the plan reports no changes beyond the import.

---

## 8. Optional follow-on: `img.zionmusicgroup.com` for R2

Once the zone is on Cloudflare, the covers bucket can get a custom domain, retiring the `r2.dev`
URL that `src/Zmg.Api/Services/R2Options.cs:17` and `README.md:57` both call out as temporary.

R2 → bucket → Settings → Custom Domains → `img.zionmusicgroup.com`. Free; Cloudflare creates the
record and certificate. Then update `R2__PublicBaseUrl` (ACA secret + `terraform.tfvars` +
dev user-secrets).

**Existing cover URLs are stored per-release in the database.** Check whether they're persisted as
absolute URLs before switching — if so, this needs a data migration or a redirect, and it becomes
its own milestone rather than a config tweak. Keep the `r2.dev` URL working until that's settled.

---

## 9. Observed but out of scope

The domain has **no SPF, no DKIM, and no DMARC records** despite running Google Workspace mail.
That's a live deliverability and spoofing exposure that exists today and is unrelated to this
migration — the migration neither causes nor fixes it. Worth handling separately. It gets *easier*
after the move, since you'll be adding TXT records in Cloudflare rather than Netlify.

---

## 10. Verification commands

Snapshot before you start, and re-run after each phase.

**Every command pins `@1.1.1.1`.** The local resolver fabricates MX records (§1) — an unpinned check
is worse than no check, because it fails loudly at exactly the moment you're primed to believe it.

```bash
for t in NS SOA A MX TXT CAA; do echo "--- $t ---"; dig +short "$t" zionmusicgroup.com @1.1.1.1; done
```

```bash
# Two independent resolvers must agree before you trust an MX result either way.
for r in 1.1.1.1 8.8.8.8 9.9.9.9; do printf '%-9s ' "$r"; dig +short MX zionmusicgroup.com @"$r"; done
```

```bash
dig +noall +answer @dns1.p04.nsone.net zionmusicgroup.com A MX && dig +noall +answer @dns1.p04.nsone.net www.zionmusicgroup.com A
```

```bash
dig +short DS zionmusicgroup.com @a.gtld-servers.net
```

```bash
dig +short A zionmusicgroup.com @1.1.1.1 && dig +short A www.zionmusicgroup.com @1.1.1.1
```

```bash
curl -sSI https://zionmusicgroup.com | head -8
```

```bash
curl -sSI https://app.zionmusicgroup.com | head -8 && curl -sS https://app.zionmusicgroup.com/api/health
```

---

## 11. Rollback

**The rollback anchor is the Netlify DNS zone.** As long as it still exists and still holds the
original records, reverting is: set the nameservers at Namecheap back to

```
dns1.p04.nsone.net   dns2.p04.nsone.net   dns3.p04.nsone.net   dns4.p04.nsone.net
```

> ✅ **Confirmed at the registrar 2026-07-27.** Namecheap → Domain List → `zionmusicgroup.com` →
> Manage → **Nameservers** is set to **Custom DNS** with exactly those four entries. That screen is
> both the Phase 2 cutover field and this rollback. Screenshot it before you change it.

That's why Phase 4 waits a week. Once you delete the Netlify zone you'd have to rebuild it from
your Phase-0 screenshot before you could roll back — take the screenshot.

Be realistic about speed: the registry NS TTL is **172800 (48h)**, so a nameserver revert is not a
fast fix. For anything that goes wrong *within* the zone, the far quicker remedy is to correct the
record in Cloudflare (record TTLs are minutes, not days). Reserve the nameserver revert for a
Cloudflare-level problem.

| Failure | Symptom | Fix |
|---|---|---|
| MX dropped or mistyped | Inbound mail silently disappears | Restore `MX 1 smtp.google.com` in Cloudflare. **Check this first, always** — it's the failure you won't notice on your own. |
| Email Routing enabled by accident | MX replaced with Cloudflare's | Disable Email Routing, restore the Google MX |
| Netlify records orange-clouded | Redirect loop, or `ERR_TOO_MANY_REDIRECTS` | Set them to DNS-only (grey), or set SSL/TLS mode to Full (strict) |
| Wrong/missing apex A | Marketing site 404s or fails to resolve | Set apex A to `75.2.60.5`, grey cloud |
| CNAME'd `app` at the old provider | `Error 1014: CNAME Cross-User Banned` | Zone isn't on Cloudflare yet — that's §2, finish the migration |
| Worker custom domain not issuing a cert | TLS error on `app.` | Wait ~15 min; confirm the name is single-label (multi-level needs paid ACM) |
| DNSSEC left enabled at cutover | **Entire domain unresolvable** | Disable DNSSEC at the registrar, wait out the DS TTL. Prevented by the §4 check. |

---

## Sources

- [Cloudflare — subdomain setup (Enterprise only)](https://developers.cloudflare.com/dns/zone-setups/subdomain-setup/)
- [Cloudflare — partial / CNAME setup (Business+)](https://developers.cloudflare.com/dns/zone-setups/partial-setup/setup/)
- [Cloudflare — Custom Domains for Workers](https://blog.cloudflare.com/custom-domains-for-workers/)
- [Cloudflare — free Universal SSL certificates](https://developers.cloudflare.com/ssl/edge-certificates/universal-ssl/)
- [Netlify — configure external DNS for a custom domain](https://docs.netlify.com/manage/domains/configure-domains/configure-external-dns/)
