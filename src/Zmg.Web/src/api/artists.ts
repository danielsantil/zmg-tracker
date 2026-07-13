import type { Artist, ArtistInput } from '@/types';
import { request } from './client';

export const artistsApi = {
  listArtists: () => request<Artist[]>('/api/artists'),
  createArtist: (input: ArtistInput) =>
    request<Artist>('/api/artists', { method: 'POST', body: JSON.stringify(input) }),
  updateArtist: (id: string, input: ArtistInput) =>
    request<Artist>(`/api/artists/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  deleteArtist: (id: string) =>
    request<void>(`/api/artists/${id}`, { method: 'DELETE' }),
};
