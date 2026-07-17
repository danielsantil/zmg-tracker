import { useEffect, useState } from 'react';

/**
 * `useState` for a small UI preference that should survive a reload — view toggles and the like,
 * persisted to `localStorage` under `zmg.<key>`.
 *
 * Every access is wrapped: `localStorage` throws on read *and* write in Safari private mode and
 * wherever site data is blocked, and a preference is never worth taking the page down for — a
 * failed read falls back to `initial`, a failed write just means it won't persist.
 *
 * `isValid` guards the parsed value, since stored data outlives the code that wrote it: a key
 * left by an older build (or hand-edited in devtools) must not load as state the UI can't render.
 */
export function usePersistedState<T>(
  key: string,
  initial: T,
  isValid: (value: unknown) => value is T,
): [T, (value: T) => void] {
  const storageKey = `zmg.${key}`;

  const [state, setState] = useState<T>(() => {
    try {
      const raw = localStorage.getItem(storageKey);
      if (raw === null) return initial;
      const parsed: unknown = JSON.parse(raw);
      return isValid(parsed) ? parsed : initial;
    } catch {
      return initial;
    }
  });

  useEffect(() => {
    try {
      localStorage.setItem(storageKey, JSON.stringify(state));
    } catch {
      // Preference just won't persist this session.
    }
  }, [storageKey, state]);

  return [state, setState];
}
