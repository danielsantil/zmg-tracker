import { translateMessage, type ServerMessage } from '@/i18n/serverText';

/**
 * Thrown for 4xx/409 responses. The wire carries `errors: [{ code, args? }]` and no prose at all
 * (M46) — `errors` keeps the raw codes so callers can *recognise* a specific failure, and `messages`
 * holds them rendered in the current language. Translating here, at construction, is what keeps
 * every catch site working on `e.message` unchanged.
 */
export class ApiError extends Error {
  readonly messages: string[];

  constructor(
    public status: number,
    public errors: ServerMessage[],
  ) {
    const messages = errors.map(translateMessage);
    super(messages[0] ?? `Request failed (${status})`);
    this.messages = messages;
  }

  /** True when the server raised this exact code — the code-aware replacement for string matching. */
  has(code: string): boolean {
    return this.errors.some((e) => e.code === code);
  }
}

/**
 * The message to show for a caught error: the server's first validation message when it's an
 * `ApiError`, else the caller's fallback. Centralizes the `e instanceof ApiError ? e.message : …`
 * shape that otherwise repeats at every catch site.
 */
export function errorMessage(e: unknown, fallback: string): string {
  return e instanceof ApiError ? e.message : fallback;
}

/**
 * Called when the server says a request needed a session and didn't have one — a session that expired
 * or was revoked mid-visit (v2.10/M56). `AuthProvider` registers this so the app flips to the login
 * gate the moment any call comes back 401, instead of the user staring at a screen whose every action
 * silently fails.
 *
 * A module-level hook rather than a React one because `request` is called from outside any component
 * tree — the same reason `ApiError` builds its messages off the i18n instance (M46).
 */
let unauthorizedHandler: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler;
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  // FormData sets its own multipart Content-Type *with the boundary* — forcing JSON here would make
  // the server unable to parse the parts (cover upload, M31).
  //
  // No locale header (v2.9): the API is language-agnostic, shipping checklist text in both languages
  // and every message as a code. Nothing it returns depends on who is asking, which is what lets a
  // language switch re-render from cache instead of refetching.
  const isFormData = init?.body instanceof FormData;
  const res = await fetch(path, {
    ...init,
    headers: isFormData ? { ...init?.headers } : { 'Content-Type': 'application/json', ...init?.headers },
  });

  if (!res.ok) {
    // Everything under /api/auth is excluded, and that exclusion is load-bearing: `/api/auth/me` is
    // the probe whose 401 *is* the answer "signed out". Letting it fire the handler would invalidate
    // the auth state, which refetches the probe, which 401s again — an infinite loop.
    if (res.status === 401 && !path.startsWith('/api/auth/')) unauthorizedHandler?.();

    // A body with no usable errors array (a 500's ProblemDetails, an HTML error page) still has to
    // render as a sentence, so fall back to a code the bundle carries rather than to English prose.
    let errors: ServerMessage[] = [{ code: 'error.unknown' }];
    try {
      const body = await res.json();
      if (Array.isArray(body?.errors) && body.errors.length > 0) errors = body.errors;
    } catch {
      /* non-JSON error body */
    }
    throw new ApiError(res.status, errors);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}
