import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { writePersisted } from '@/hooks/usePersistedState';
import { LANGUAGE_KEY, type Language, resolveInitialLanguage } from './language';

/**
 * UI language state, deliberately mirroring `hooks/useTheme.ts` — including its rule that a choice is
 * persisted **only on an explicit change**, so a first-time visitor keeps following their browser
 * until they actually pick. Reflects the value onto `i18next` and onto `<html lang>` (which the
 * pre-paint script in index.html already stamped).
 */
export function useLanguage(): { language: Language; setLanguage: (next: Language) => void } {
  const { i18n } = useTranslation();
  const queryClient = useQueryClient();
  const [language, setLanguageState] = useState<Language>(resolveInitialLanguage);

  useEffect(() => {
    void i18n.changeLanguage(language);
    document.documentElement.lang = language;
  }, [i18n, language]);

  const setLanguage = useCallback(
    (next: Language) => {
      writePersisted(LANGUAGE_KEY, next);
      setLanguageState(next);
      // Since M47 the server answers checklist text per locale, keyed off the `X-Lang` header
      // `client.ts` sends — so every cached payload is now in the *previous* language. Chrome text
      // re-renders on its own; this is what makes the task titles follow.
      void queryClient.invalidateQueries();
    },
    [queryClient],
  );

  return { language, setLanguage };
}
