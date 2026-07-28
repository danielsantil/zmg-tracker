import type { ReactNode } from 'react';
import { useAuth } from './useAuth';
import LoginPage from '@/features/auth/LoginPage';

/**
 * One gate around the entire app (v2.10/M56).
 *
 * Authorization is flat — on the whitelist means full access — so there is nothing to guard *per
 * route*. A route-by-route guard would be ceremony that can only ever be wrong in one direction, and
 * the direction it fails in is "left a page unprotected".
 *
 * Rendering the login screen *in place*, without navigating, is what makes `returnUrl` unnecessary on
 * the client: the URL never changes, so after signing in the browser is already where the partner was
 * trying to go. The API is the real boundary regardless — every `/api/*` call answers 401 without a
 * session, so this gate is a UX affordance rather than a security control.
 */
export function AuthGate({ children }: { children: ReactNode }) {
  const { status } = useAuth();

  // A blank frame, not a spinner: the probe resolves in milliseconds against a warm container, and a
  // spinner that flashes for 40ms reads as jank. The container's cold start is the slow case, and the
  // Worker already paints the shell for that (M42).
  if (status === 'loading') return <div className="min-h-screen" aria-busy="true" />;

  if (status === 'out') return <LoginPage />;

  return <>{children}</>;
}
