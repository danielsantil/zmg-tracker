import type { PhaseReorderInput, TaskAddInput, Template, TemplateTaskDto, TemplateTaskUpdateInput } from '@/types';
import { request } from './client';

// Templates (template management)
export const templatesApi = {
  list: () => request<Template[]>('/api/templates'),
  addTask: (templateId: string, input: TaskAddInput) =>
    request<TemplateTaskDto>(`/api/templates/${templateId}/tasks`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),
  updateTask: (id: string, input: TemplateTaskUpdateInput) =>
    request<TemplateTaskDto>(`/api/template-tasks/${id}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  reorderTasks: (templateId: string, input: PhaseReorderInput) =>
    request<void>(`/api/templates/${templateId}/tasks/order`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),
  deleteTask: (id: string) =>
    request<void>(`/api/template-tasks/${id}`, { method: 'DELETE' }),
};
