import { createContext, useContext } from 'react';
import type { AuthUser } from '@/types';

export type AuthState = {
  /** `loading` until the probe answers; then `in` or `out`. */
  status: 'loading' | 'in' | 'out';
  user: AuthUser | null;
  /** Full-page navigation to the server's sign-in entry point. */
  signIn: () => void;
  signOut: () => Promise<void>;
};

// The context lives here (apart from <AuthProvider>) so the provider file only exports a component —
// keeping Fast Refresh happy — while call sites still `useAuth()` from one place. Same split as
// useConfirm/ConfirmProvider.
export const AuthContext = createContext<AuthState | null>(null);

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside an <AuthProvider>');
  return ctx;
}
