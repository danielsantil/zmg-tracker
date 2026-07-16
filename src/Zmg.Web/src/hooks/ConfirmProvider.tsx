import { useCallback, useRef, useState, type ReactNode } from 'react';
import { ConfirmDialog, type ConfirmOptions } from '@/components';
import { ConfirmContext, type ConfirmFn } from './useConfirm';

/**
 * Mounts the app's one <ConfirmDialog> and exposes a promise-based `confirm(opts)` —
 * a branded stand-in for `window.confirm`, so call sites keep reading
 * `if (!(await confirm({...}))) return;`. Mount once, at the App root.
 */
export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [options, setOptions] = useState<ConfirmOptions | null>(null);
  // Held outside state so resolving stays a plain side effect, never a state updater.
  const resolver = useRef<((confirmed: boolean) => void) | null>(null);

  const confirm = useCallback<ConfirmFn>(
    (next) =>
      new Promise<boolean>((resolve) => {
        resolver.current?.(false); // a dialog opened over another one cancels the first
        resolver.current = resolve;
        setOptions(next);
      }),
    [],
  );

  const onResolve = useCallback((confirmed: boolean) => {
    resolver.current?.(confirmed);
    resolver.current = null;
    setOptions(null);
  }, []);

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      <ConfirmDialog options={options} onResolve={onResolve} />
    </ConfirmContext.Provider>
  );
}
