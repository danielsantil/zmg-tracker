import type { AuthUser } from '@/types';
import { ApiError, request } from './client';

export const authApi = {
  /**
   * The auth probe. Resolves to `null` when signed out rather than throwing, because "not signed in"
   * is an ordinary answer here, not a failure — it is precisely what this endpoint exists to report.
   * Turning it into a rejection would make every caller unwrap an error to read a boolean.
   *
   * Any *other* status still throws: a 500 on the probe is a real problem and must not be
   * indistinguishable from being logged out.
   */
  me: async (): Promise<AuthUser | null> => {
    try {
      return await request<AuthUser>('/api/auth/me');
    } catch (e) {
      if (e instanceof ApiError && e.status === 401) return null;
      throw e;
    }
  },

  /** Deletes the session row server-side, not just the cookie. */
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
};
