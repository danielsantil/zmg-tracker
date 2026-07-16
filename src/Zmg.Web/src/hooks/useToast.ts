import { useCallback, useEffect, useRef, useState } from 'react';
import type { ToastVariant } from '@/components';

/**
 * Transient toast state: `showToast(msg)` displays a message for 3s, replacing any
 * in-flight one. The timer is cleared on unmount. Pair with the <Toast> component.
 * Variant defaults to 'error' — the common case is a revert/failure message.
 */
export function useToast() {
  const [toast, setToast] = useState<{ message: string; variant: ToastVariant } | null>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const showToast = useCallback((msg: string, variant: ToastVariant = 'error') => {
    setToast({ message: msg, variant });
    if (timer.current) clearTimeout(timer.current);
    timer.current = setTimeout(() => setToast(null), 3000);
  }, []);

  useEffect(
    () => () => {
      if (timer.current) clearTimeout(timer.current);
    },
    [],
  );

  return { toast: toast?.message ?? null, toastVariant: toast?.variant ?? 'error', showToast };
}
