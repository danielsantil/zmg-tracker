import { persistKey, readPersisted } from '@/hooks/usePersistedState';

/**
 * The languages the UI ships (v2.8). Adding one means adding a `locales/<code>.json` and a name
 * below — never touching component code. `es` is unqualified on purpose: `Intl` resolves it and ZMG
 * has no regional-format requirement.
 */
export const SUPPORTED_LANGUAGES = ['en', 'es'] as const;

export type Language = (typeof SUPPORTED_LANGUAGES)[number];

/** Each language's name *in its own language* — the accessible convention for a language switcher. */
export const LANGUAGE_NAMES: Record<Language, string> = {
  en: 'English',
  es: 'Español',
};

export const LANGUAGE_KEY = persistKey('lang');

export const isLanguage = (v: unknown): v is Language =>
  typeof v === 'string' && (SUPPORTED_LANGUAGES as readonly string[]).includes(v);

/**
 * The browser's preferred language, narrowed to one we ship. Only the primary subtag is considered
 * (`es-MX` → `es`); anything unsupported falls back to English. Wrapped like `useTheme`'s
 * `systemTheme()` — `navigator` is absent in the node test env.
 */
export function browserLanguage(): Language {
  try {
    const primary = navigator.language.split('-')[0];
    return isLanguage(primary) ? primary : 'en';
  } catch {
    return 'en';
  }
}

/**
 * The language to render on load: a saved choice wins, otherwise follow the browser. Mirrors the
 * inline no-flash script in index.html (which stamps `<html lang>` before React mounts) — keep the
 * two in sync.
 */
export function resolveInitialLanguage(): Language {
  return readPersisted(LANGUAGE_KEY, browserLanguage(), isLanguage);
}
