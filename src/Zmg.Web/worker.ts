// Only runs for /api/* — wrangler.jsonc's run_worker_first routes everything else straight to the
// static assets, which never touch this code. Forwards the request to the ACA container, preserving
// method, headers and body, so the SPA keeps calling same-origin /api paths.
export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const proxied = new Request(new URL(url.pathname + url.search, env.API_ORIGIN), request);

    // Rewriting the URL to API_ORIGIN also rewrites `Host` — it has to, because ACA's ingress routes
    // on it and rejects anything else. That throws away the public hostname, which the API needs:
    // ASP.NET builds the Google OIDC `redirect_uri` from Request.Scheme + Request.Host (v2.10/M55).
    // Unfixed, it would send Google a redirect_uri pointing at the ACA FQDN, Google would reject it
    // as unregistered, and the error would name neither this Worker nor the Host header.
    //
    // `set`, never `append`: a client-supplied X-Forwarded-* is overwritten rather than trusted, so
    // these headers say what the edge saw. The API additionally pins Auth:AllowedHosts, because the
    // ACA FQDN stays publicly reachable and a forwarded host is otherwise forgeable there.
    proxied.headers.set('X-Forwarded-Host', url.host);
    proxied.headers.set('X-Forwarded-Proto', url.protocol.replace(':', ''));

    return fetch(proxied);
  },
};
