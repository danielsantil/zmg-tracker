import type { PendingAction } from '@/types';
import { request } from './client';

// Pending-actions engine (M10)
export const pendingApi = {
  list: () => request<PendingAction[]>('/api/pending'),
  listByRelease: (releaseId: string) =>
    request<PendingAction[]>(`/api/pending/${releaseId}`),
};
