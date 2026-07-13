import type { TrackDto } from '@/types';
import { request } from './client';

// Tracks (album support)
export const tracksApi = {
  add: (releaseId: string, input: { title: string }) =>
    request<TrackDto>(`/api/releases/${releaseId}/tracks`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  update: (id: string, input: { title: string; isFocusTrack: boolean }) =>
    request<TrackDto>(`/api/tracks/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  toggleFocus: (id: string) =>
    request<TrackDto>(`/api/tracks/${id}/focus`, { method: 'PATCH' }),
  reorder: (releaseId: string, input: { orderedTrackIds: string[] }) =>
    request<void>(`/api/releases/${releaseId}/tracks/order`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  delete: (id: string) =>
    request<void>(`/api/tracks/${id}`, { method: 'DELETE' }),
};
