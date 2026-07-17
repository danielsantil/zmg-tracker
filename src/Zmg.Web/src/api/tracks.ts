import type { TrackDto, TrackInput, TrackReorderInput } from '@/types';
import { request } from './client';

// Tracks (v2.0: a Release↔Song join, addressed by songId, all under the release group).
export const tracksApi = {
  add: (releaseId: string, input: TrackInput) =>
    request<TrackDto>(`/api/releases/${releaseId}/tracks`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  toggleFocus: (releaseId: string, songId: string) =>
    request<TrackDto>(`/api/releases/${releaseId}/tracks/${songId}/focus`, { method: 'PATCH' }),
  reorder: (releaseId: string, input: TrackReorderInput) =>
    request<void>(`/api/releases/${releaseId}/tracks/order`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  delete: (releaseId: string, songId: string) =>
    request<void>(`/api/releases/${releaseId}/tracks/${songId}`, { method: 'DELETE' }),
};
