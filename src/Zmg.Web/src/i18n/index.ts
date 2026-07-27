import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en.json';
import es from './locales/es.json';
import { SUPPORTED_LANGUAGES, resolveInitialLanguage } from './language';

/**
 * i18next setup (v2.8). Translations are **bundled**, not fetched: both languages are ~10KB gzipped
 * each, so an `i18next-http-backend` round-trip would undo M42's edge-served first paint to save
 * almost nothing. One namespace with nested keys — with nothing to lazy-load, per-feature namespaces
 * would only add `useTranslation('x')` ceremony at every call site.
 *
 * Imported once from `main.tsx` *before* `<App />` renders, so no component sees an uninitialized
 * instance. Language detection is `language.ts` (six lines) rather than the detector plugin, which
 * would have to be reconciled with `usePersistedState` anyway.
 */
void i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    es: { translation: es },
  },
  lng: resolveInitialLanguage(),
  fallbackLng: 'en',
  supportedLngs: SUPPORTED_LANGUAGES,
  // React escapes interpolated values already; escaping here would double-encode.
  interpolation: { escapeValue: false },
  returnNull: false,
});

export default i18n;
