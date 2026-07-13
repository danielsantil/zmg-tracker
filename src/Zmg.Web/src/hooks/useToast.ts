import { useCallback, useEffect, useRef, useState } from 'react';

/**
 * Transient toast state: `showToast(msg)` displays a message for 3s, replacing any
 * in-flight one. The timer is cleared on unmount. Pair with the <Toast> component.
 */
export function useToast() {
  const [toast, setToast] = useState<string | null>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const showToast = useCallback((msg: string) => {
    setToast(msg);
    if (timer.current) clearTimeout(timer.current);
    timer.current = setTimeout(() => setToast(null), 3000);
  }, []);

  useEffect(
    () => () => {
      if (timer.current) clearTimeout(timer.current);
    },
    [],
  );

  return { toast, showToast };
}
