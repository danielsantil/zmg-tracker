import type { Phase, ReleaseTaskDto } from '@/types';
import { request } from './client';

// Release tasks (checklist engine)
export const tasksApi = {
  add: (
    releaseId: string,
    input: { title: string; phase: Phase; minDaysBefore?: number | null; maxDaysBefore?: number | null },
  ) =>
    request<ReleaseTaskDto>(`/api/releases/${releaseId}/tasks`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  update: (
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
  toggle: (id: string) =>
    request<ReleaseTaskDto>(`/api/tasks/${id}/toggle`, { method: 'PATCH' }),
  reorder: (releaseId: string, input: { phase: Phase; orderedTaskIds: string[] }) =>
    request<void>(`/api/releases/${releaseId}/tasks/order`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  delete: (id: string) =>
    request<void>(`/api/tasks/${id}`, { method: 'DELETE' }),
};
