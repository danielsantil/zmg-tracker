import { useCallback, useEffect, useState } from 'react';
import { persistKey, readPersisted, writePersisted } from './usePersistedState';

export type Theme = 'light' | 'dark';

const THEME_KEY = persistKey('theme');

export const isTheme = (v: unknown): v is Theme => v === 'light' || v === 'dark';

/**
 * The OS preference, defaulting to dark where `matchMedia` is unavailable (the node test env, SSR,
 * old browsers). Reads `globalThis.matchMedia` — equal to `window.matchMedia` in the browser — so the
 * function is environment-agnostic.
 */
function systemTheme(): Theme {
  try {
    return globalThis.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  } catch {
    return 'dark';
  }
}

/**
 * The theme to paint on load: a saved choice wins, otherwise follow the OS. Mirrors the inline
 * no-flash script in index.html (which sets `data-theme` before React mounts) — keep the two in sync.
 */
export function resolveInitialTheme(): Theme {
  return readPersisted(THEME_KEY, systemTheme(), isTheme);
}

function apply(theme: Theme): void {
  document.documentElement.dataset.theme = theme;
}

/**
 * Dark/light theme state. Seeds from `resolveInitialTheme` and reflects the value onto
 * `<html data-theme>` (the token channels in index.css key off it). A choice is persisted **only on
 * an explicit toggle**, so a first-time visitor keeps following their OS until they actually pick —
 * we never write the system default back as if it were a decision.
 */
export function useTheme(): { theme: Theme; toggle: () => void } {
  const [theme, setTheme] = useState<Theme>(resolveInitialTheme);

  useEffect(() => {
    apply(theme);
  }, [theme]);

  const toggle = useCallback(() => {
    setTheme((prev) => {
      const next: Theme = prev === 'dark' ? 'light' : 'dark';
      writePersisted(THEME_KEY, next);
      return next;
    });
  }, []);

  return { theme, toggle };
}
