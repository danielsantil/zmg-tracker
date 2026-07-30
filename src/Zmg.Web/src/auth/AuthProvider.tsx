import { useCallback, useEffect, useMemo, type ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api';
import { setUnauthorizedHandler } from '@/api/client';
import { loginUrl } from '@/lib/returnUrl';
import { AuthContext, type AuthState } from './useAuth';

const authKey = ['auth', 'me'] as const;

/**
 * Holds "who is signed in", derived from a single `GET /api/auth/me` (v2.10/M56).
 *
 * The probe is a normal query so it shares the cache and dedups, but capped at one retry — a 401 is
 * an answer, not a transient failure worth retrying three times before the login screen appears.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();

  const { data, isPending } = useQuery({
    queryKey: authKey,
    queryFn: () => api.auth.me(),
    // `me()` resolves a 401 to null rather than throwing, so anything that *does* throw here is a real
    // failure — a network blip or a 500 — and is worth one retry. Retrying is not a way of waiting out
    // a cold start: ACA holds the request while the container activates, so that path is slow, not
    // failing.
    retry: 1,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });

  // Any *other* endpoint answering 401 means the session expired or was revoked while the tab was
  // open. Writing the cache directly rather than invalidating flips the gate without a refetch that
  // would only 401 again.
  useEffect(() => {
    setUnauthorizedHandler(() => queryClient.setQueryData(authKey, null));
    return () => setUnauthorizedHandler(null);
  }, [queryClient]);

  const signIn = useCallback(() => {
    // Deliberately a browser navigation, not fetch: this is a redirect chain through Google's consent
    // screen, and the whole point of the BFF is that the token exchange happens server-side.
    window.location.assign(loginUrl());
  }, []);

  const signOut = useCallback(async () => {
    await api.auth.logout();
    queryClient.setQueryData(authKey, null);
    // Drop every cached release, song and artist. Leaving them would show the previous person's data
    // behind the login screen and, worse, to whoever signs in next on a shared machine.
    queryClient.clear();
  }, [queryClient]);

  const value = useMemo<AuthState>(
    () => ({
      status: isPending ? 'loading' : data ? 'in' : 'out',
      user: data ?? null,
      signIn,
      signOut,
    }),
    [isPending, data, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
