import type {
  Artist,
  ArtistInput,
  CreatedWithWarnings,
  Phase,
  ReleaseDetail,
  ReleaseInput,
  ReleaseListItem,
  ReleaseTaskDto,
  ReleaseType,
} from './types';

/** Thrown for 4xx/409 responses; carries the server's validation error messages. */
export class ApiError extends Error {
  constructor(
    public status: number,
    public errors: string[],
  ) {
    super(errors[0] ?? `Request failed (${status})`);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
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

export const api = {
  // Artists
  listArtists: () => request<Artist[]>('/api/artists'),
  createArtist: (input: ArtistInput) =>
    request<Artist>('/api/artists', { method: 'POST', body: JSON.stringify(input) }),
  updateArtist: (id: string, input: ArtistInput) =>
    request<Artist>(`/api/artists/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  deleteArtist: (id: string) =>
    request<void>(`/api/artists/${id}`, { method: 'DELETE' }),

  // Releases
  listReleases: (filters?: { artistId?: string; type?: ReleaseType; status?: string }) => {
    const qs = new URLSearchParams();
    if (filters?.artistId) qs.set('artistId', filters.artistId);
    if (filters?.type !== undefined) qs.set('type', String(filters.type));
    if (filters?.status) qs.set('status', filters.status);
    const suffix = qs.toString() ? `?${qs}` : '';
    return request<ReleaseListItem[]>(`/api/releases${suffix}`);
  },
  getRelease: (id: string) => request<ReleaseDetail>(`/api/releases/${id}`),
  createRelease: (input: ReleaseInput) =>
    request<CreatedWithWarnings<ReleaseDetail>>('/api/releases', {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  updateRelease: (id: string, input: ReleaseInput) =>
    request<CreatedWithWarnings<ReleaseDetail>>(`/api/releases/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  deleteRelease: (id: string) =>
    request<void>(`/api/releases/${id}`, { method: 'DELETE' }),

  // Release tasks (checklist engine)
  addTask: (releaseId: string, input: { title: string; phase: Phase }) =>
    request<ReleaseTaskDto>(`/api/releases/${releaseId}/tasks`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  updateTask: (id: string, input: { title: string; phase: Phase; notes: string | null }) =>
    request<ReleaseTaskDto>(`/api/tasks/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  toggleTask: (id: string) =>
    request<ReleaseTaskDto>(`/api/tasks/${id}/toggle`, { method: 'PATCH' }),
  reorderTasks: (releaseId: string, input: { phase: Phase; orderedTaskIds: string[] }) =>
    request<void>(`/api/releases/${releaseId}/tasks/order`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  deleteTask: (id: string) =>
    request<void>(`/api/tasks/${id}`, { method: 'DELETE' }),
};
