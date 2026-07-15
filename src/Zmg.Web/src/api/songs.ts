import type { CreatedWithWarnings, SongDetail, SongListItem, SongUpdateInput } from '@/types';
import { request } from './client';

// Catalog (M13).
export const songsApi = {
  list: (filters?: { q?: string; scope?: 'all' | 'archived' }) => {
    const qs = new URLSearchParams();
    if (filters?.q) qs.set('q', filters.q);
    if (filters?.scope) qs.set('scope', filters.scope);
    const suffix = qs.toString() ? `?${qs}` : '';
    return request<SongListItem[]>(`/api/songs${suffix}`);
  },
  get: (id: string) => request<SongDetail>(`/api/songs/${id}`),
  update: (id: string, input: SongUpdateInput) =>
    request<CreatedWithWarnings<SongDetail>>(`/api/songs/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
};
