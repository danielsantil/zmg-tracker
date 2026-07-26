// Only runs for /api/* — wrangler.jsonc's run_worker_first routes everything else straight to the
// static assets, which never touch this code. Forwards the request to the ACA container unchanged,
// preserving method, headers and body, so the SPA keeps calling same-origin /api paths.
export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    return fetch(new Request(new URL(url.pathname + url.search, env.API_ORIGIN), request));
  },
};