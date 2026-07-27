import { useCallback, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { writePersisted } from '@/hooks/usePersistedState';
import { LANGUAGE_KEY, type Language, isLanguage } from './language';

/**
 * UI language state. Unlike `hooks/useTheme.ts` — which it otherwise mirrors, including the rule that
 * a choice is persisted **only on an explicit change** — this holds **no `useState` of its own**:
 * i18next already owns the current language, and `useTranslation` re-renders on `languageChanged`, so
 * reading the instance is both reactive and the single source of truth.
 *
 * That matters because the hook has more than one call site (the navbar toggle, and the templates
 * screen's per-locale hint). A mirrored `useState` gave each call site its own copy, and whichever
 * one's effect ran last wrote its **stale** value back into i18next — clicking the toggle on
 * `/templates` persisted the new language and then immediately reverted the app to the old one. Any
 * number of call sites can use this version; they all read the same value and none of them can fight.
 *
 * The initial value needs no resolution here: `i18n/index.ts` inits with `resolveInitialLanguage()`,
 * matching the pre-paint script in index.html that already stamped `<html lang>`.
 */
export function useLanguage(): { language: Language; setLanguage: (next: Language) => void } {
  const { i18n } = useTranslation();

  const language: Language = isLanguage(i18n.language) ? i18n.language : 'en';

  // Idempotent, so it's safe from every call site: they all write the same value.
  useEffect(() => {
    document.documentElement.lang = language;
  }, [language]);

  const setLanguage = useCallback(
    (next: Language) => {
      writePersisted(LANGUAGE_KEY, next);
      // Just the language — no cache invalidation (v2.9). Server payloads carry both languages and
      // every message as a code, so nothing cached is stale and a switch is a pure re-render.
      //
      // This used to invalidate every query, and the ordering was load-bearing and easy to get wrong:
      // invalidating before `changeLanguage` refetched the *old* locale, so the chrome flipped and the
      // checklist didn't. There is no ordering to get wrong when there is no refetch.
      void i18n.changeLanguage(next);
    },
    [i18n],
  );

  return { language, setLanguage };
}
