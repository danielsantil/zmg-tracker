import type { Phase, Template, TemplateTaskDto } from '@/types';
import { request } from './client';

// Templates (template management)
export const templatesApi = {
  list: () => request<Template[]>('/api/templates'),
  addTask: (
    templateId: string,
    input: { title: string; phase: Phase; minDaysBefore?: number | null; maxDaysBefore?: number | null },
  ) =>
    request<TemplateTaskDto>(`/api/templates/${templateId}/tasks`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  updateTask: (
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
  reorderTasks: (templateId: string, input: { phase: Phase; orderedTaskIds: string[] }) =>
    request<void>(`/api/templates/${templateId}/tasks/order`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  deleteTask: (id: string) =>
    request<void>(`/api/template-tasks/${id}`, { method: 'DELETE' }),
};
