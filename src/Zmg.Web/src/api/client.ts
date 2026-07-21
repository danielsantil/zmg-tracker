/** Thrown for 4xx/409 responses; carries the server's validation error messages. */
export class ApiError extends Error {
  constructor(
    public status: number,
    public errors: string[],
  ) {
    super(errors[0] ?? `Request failed (${status})`);
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
  // the server unable to parse the parts (cover upload, M31).
  const isFormData = init?.body instanceof FormData;
  const res = await fetch(path, {
    ...init,
    headers: isFormData ? init?.headers : { 'Content-Type': 'application/json', ...init?.headers },
  });

  if (!res.ok) {
    let errors: string[] = [`Request failed (${res.status})`];
    try {
      const body = await res.json();
      if (Array.isArray(body?.errors)) errors = body.errors;
    } catch {
      /* non-JSON error body */
    }
    throw new ApiError(res.status, errors);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}
