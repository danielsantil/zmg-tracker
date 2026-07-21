import type { UploadedCover } from '@/types';
import { request } from './client';

/**
 * Cover ingest (M31). Both paths end with the image stored in R2 — a pasted URL is fetched by the
 * server, never hotlinked — so both return the R2 URL to save as the release's `coverUrl`.
 */
export const uploadsApi = {
  cover: (file: File) => {
    const body = new FormData();
    body.append('file', file);
    return request<UploadedCover>('/api/uploads/cover', { method: 'POST', body });
  },
  coverFromUrl: (url: string) =>
    request<UploadedCover>('/api/uploads/cover-from-url', {
      method: 'POST',
      body: JSON.stringify({ url }),
    }),
};
