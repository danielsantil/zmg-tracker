import type { PendingAction } from '@/types';
import { request } from './client';

// Pending-actions engine (M10)
export const pendingApi = {
  listPending: () => request<PendingAction[]>('/api/pending'),
  listPendingByRelease: (releaseId: string) =>
    request<PendingAction[]>(`/api/pending/${releaseId}`),
};
