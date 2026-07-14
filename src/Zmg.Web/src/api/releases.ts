import type {
  CreatedWithWarnings,
  ReleaseDetail,
  ReleaseInput,
  ReleaseListItem,
  ReleaseType,
} from '@/types';
import { request } from './client';

export const releasesApi = {
  list: (filters?: {
    artistId?: string;
    type?: ReleaseType;
    status?: string;
    scope?: 'home' | 'all' | 'archived';
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
  get: (id: string) => request<ReleaseDetail>(`/api/releases/${id}`),
  create: (input: ReleaseInput) =>
    request<CreatedWithWarnings<ReleaseDetail>>('/api/releases', {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  update: (id: string, input: ReleaseInput) =>
    request<CreatedWithWarnings<ReleaseDetail>>(`/api/releases/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  // Archive a release (terminal, non-restorable). v1.2.
  archive: (id: string) =>
    request<void>(`/api/releases/${id}/archive`, { method: 'POST' }),
  // Remove an archived release — soft-delete on the server (never hard-deleted). v1.2.
  delete: (id: string) =>
    request<void>(`/api/releases/${id}`, { method: 'DELETE' }),
};
