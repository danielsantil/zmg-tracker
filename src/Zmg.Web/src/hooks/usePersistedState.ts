import { useEffect, useState } from 'react';

/** The `zmg.`-prefixed localStorage key for a preference. */
export const persistKey = (key: string) => `zmg.${key}`;

/**
 * Read a persisted preference. A missing key, unparseable JSON, or a value the `isValid` guard
 * rejects (stored data outlives the code that wrote it) all fall back to `initial`; a throwing
 * `localStorage` (Safari private mode, blocked site data) does too. Pure — the testable core.
 */
export function readPersisted<T>(
  storageKey: string,
  initial: T,
  isValid: (value: unknown) => value is T,
): T {
  try {
    const raw = localStorage.getItem(storageKey);
    if (raw === null) return initial;
    const parsed: unknown = JSON.parse(raw);
    return isValid(parsed) ? parsed : initial;
  } catch {
    return initial;
  }
}

/** Write a persisted preference, swallowing a throwing `localStorage` — it just won't persist. */
export function writePersisted<T>(storageKey: string, value: T): void {
  try {
    localStorage.setItem(storageKey, JSON.stringify(value));
  } catch {
    // Preference just won't persist this session.
  }
}

/**
 * `useState` for a small UI preference that should survive a reload — view toggles and the like,
 * persisted to `localStorage` under `zmg.<key>`.
 *
 * Every access is wrapped (see `readPersisted`/`writePersisted`): `localStorage` throws on read
 * *and* write in Safari private mode and wherever site data is blocked, and a preference is never
 * worth taking the page down for — a failed read falls back to `initial`, a failed write just
 * means it won't persist.
 *
 * `isValid` guards the parsed value, since stored data outlives the code that wrote it: a key
 * left by an older build (or hand-edited in devtools) must not load as state the UI can't render.
 */
export function usePersistedState<T>(
  key: string,
  initial: T,
  isValid: (value: unknown) => value is T,
): [T, (value: T) => void] {
  const storageKey = persistKey(key);

  const [state, setState] = useState<T>(() => readPersisted(storageKey, initial, isValid));

  useEffect(() => {
    writePersisted(storageKey, state);
  }, [storageKey, state]);

  return [state, setState];
}
