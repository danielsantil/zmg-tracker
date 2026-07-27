import i18n from '@/i18n';
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

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  // FormData sets its own multipart Content-Type *with the boundary* — forcing JSON here would make
  // the server unable to parse the parts (cover upload, M31). X-Lang rides on *both* branches: the
  // API resolves checklist text per locale (M47), and a request that skips the header silently
  // answers in English.
  const isFormData = init?.body instanceof FormData;
  const localeHeader = { 'X-Lang': i18n.language };
  const res = await fetch(path, {
    ...init,
    headers: isFormData
      ? { ...localeHeader, ...init?.headers }
      : { 'Content-Type': 'application/json', ...localeHeader, ...init?.headers },
  });

  if (!res.ok) {
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
