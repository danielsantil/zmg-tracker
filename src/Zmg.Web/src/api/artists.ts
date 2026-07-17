import type { Artist, ArtistInput } from '@/types';
import { request } from './client';

export const artistsApi = {
  list: () => request<Artist[]>('/api/artists'),
  get: (id: string) => request<Artist>(`/api/artists/${id}`),
  create: (input: ArtistInput) =>
    request<Artist>('/api/artists', { method: 'POST', body: JSON.stringify(input) }),
  update: (id: string, input: ArtistInput) =>
    request<Artist>(`/api/artists/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  delete: (id: string) =>
    request<void>(`/api/artists/${id}`, { method: 'DELETE' }),
};
