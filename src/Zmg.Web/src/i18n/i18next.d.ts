import 'i18next';
import type en from './locales/en.json';

/**
 * Types `t()` off the English resource file, so a typo'd or missing key is a **compile error** rather
 * than a string that silently renders as its own key path. English is the shape of record; `es.json`
 * is kept in step by the key-parity test in `i18n.test.ts` (types can't see it — it isn't the default
 * namespace's source).
 */
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation';
    resources: {
      translation: typeof en;
    };
  }
}
