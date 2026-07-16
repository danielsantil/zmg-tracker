import { createContext, useContext } from 'react';
import type { ConfirmOptions } from '@/components';

export type ConfirmFn = (options: ConfirmOptions) => Promise<boolean>;

// The context lives here (apart from <ConfirmProvider>) so the provider file only exports a
// component — keeping Fast Refresh happy — while call sites still `useConfirm()` from one place.
export const ConfirmContext = createContext<ConfirmFn | null>(null);

export function useConfirm(): ConfirmFn {
  const confirm = useContext(ConfirmContext);
  if (!confirm) throw new Error('useConfirm must be used inside a <ConfirmProvider>');
  return confirm;
}
