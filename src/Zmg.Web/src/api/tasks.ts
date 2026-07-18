import type { PhaseReorderInput, ReleaseTaskDto, ReleaseTaskUpdateInput, TaskAddInput } from '@/types';
import { request } from './client';

// Release tasks (checklist engine)
export const tasksApi = {
  add: (releaseId: string, input: TaskAddInput) =>
    request<ReleaseTaskDto>(`/api/releases/${releaseId}/tasks`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  update: (id: string, input: ReleaseTaskUpdateInput) =>
    request<ReleaseTaskDto>(`/api/tasks/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  toggle: (id: string) =>
    request<ReleaseTaskDto>(`/api/tasks/${id}/toggle`, { method: 'PATCH' }),
  reorder: (releaseId: string, input: PhaseReorderInput) =>
    request<void>(`/api/releases/${releaseId}/tasks/order`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  delete: (id: string) =>
    request<void>(`/api/tasks/${id}`, { method: 'DELETE' }),
};
