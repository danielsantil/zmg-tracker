import type {
  Artist,
  ArtistInput,
  CreatedWithWarnings,
  Phase,
  ReleaseDetail,
  PendingAction,
  ReleaseInput,
  ReleaseListItem,
  ReleaseTaskDto,
  ReleaseType,
  Template,
  TemplateTaskDto,
  TrackDto,
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
  listReleases: (filters?: {
    artistId?: string;
    type?: ReleaseType;
    status?: string;
    scope?: 'home' | 'all';
    q?: string;
  }) => {
    const qs = new URLSearchParams();
    if (filters?.artistId) qs.set('artistId', filters.artistId);
    if (filters?.type !== undefined) qs.set('type', String(filters.type));
    if (filters?.status) qs.set('status', filters.status);
    if (filters?.scope) qs.set('scope', filters.scope);
    if (filters?.q) qs.set('q', filters.q);
    const suffix = qs.toString() ? `?${qs}` : '';
    return request<ReleaseListItem[]>(`/api/releases${suffix}`);
  },
  listPending: () => request<PendingAction[]>('/api/pending'),
  listPendingByRelease: (releaseId: string) => request<PendingAction[]>(`/api/pending/${releaseId}`),
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
  addTask: (
    releaseId: string,
    input: { title: string; phase: Phase; minDaysBefore?: number | null; maxDaysBefore?: number | null },
  ) =>
    request<ReleaseTaskDto>(`/api/releases/${releaseId}/tasks`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  updateTask: (
    id: string,
    input: {
      title: string;
      phase: Phase;
      notes: string | null;
      minDaysBefore: number | null;
      maxDaysBefore: number | null;
    },
  ) =>
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

  // Templates (template management)
  listTemplates: () => request<Template[]>('/api/templates'),
  addTemplateTask: (
    templateId: string,
    input: { title: string; phase: Phase; minDaysBefore?: number | null; maxDaysBefore?: number | null },
  ) =>
    request<TemplateTaskDto>(`/api/templates/${templateId}/tasks`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  updateTemplateTask: (
    id: string,
    input: {
      title: string;
      phase: Phase;
      minDaysBefore: number | null;
      maxDaysBefore: number | null;
    },
  ) =>
    request<TemplateTaskDto>(`/api/template-tasks/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  reorderTemplateTasks: (templateId: string, input: { phase: Phase; orderedTaskIds: string[] }) =>
    request<void>(`/api/templates/${templateId}/tasks/order`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  deleteTemplateTask: (id: string) =>
    request<void>(`/api/template-tasks/${id}`, { method: 'DELETE' }),

  // Tracks (album support)
  addTrack: (releaseId: string, input: { title: string }) =>
    request<TrackDto>(`/api/releases/${releaseId}/tracks`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  updateTrack: (id: string, input: { title: string; isFocusTrack: boolean }) =>
    request<TrackDto>(`/api/tracks/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  toggleTrackFocus: (id: string) =>
    request<TrackDto>(`/api/tracks/${id}/focus`, { method: 'PATCH' }),
  reorderTracks: (releaseId: string, input: { orderedTrackIds: string[] }) =>
    request<void>(`/api/releases/${releaseId}/tracks/order`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  deleteTrack: (id: string) =>
    request<void>(`/api/tracks/${id}`, { method: 'DELETE' }),
};
