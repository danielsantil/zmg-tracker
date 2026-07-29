import { useTranslation } from 'react-i18next';
import { Globe } from 'lucide-react';
import { LANGUAGE_NAMES, type Language } from '@/i18n/language';

/**
 * The UI language switcher (deferred from M37, landed in M43). The visible code is the language
 * **currently being displayed** — it reads as a state indicator, which is what users expect of a
 * language switcher and is the one place this deliberately diverges from `ThemeToggle`'s "shows what
 * you'd switch TO". Clicking still switches to the other language, after which the code shows *that*
 * one. With exactly two languages a toggle beats a dropdown.
 *
 * The `aria-label` describes the **action**, not the state, and names the target language in its own
 * language (`Español`, not `Spanish`) — the accessible convention for a language switcher.
 *
 * Presentational: state comes from `useLanguage`, like `ThemeToggle`'s theme. If a third language ever
 * lands this becomes a popover — which must portal to `<body>` per the standing rule, since it would
 * sit inside a transform-free but stacking-context'd header.
 */
export function LanguageToggle({
  language,
  setLanguage,
}: {
  language: Language;
  setLanguage: (next: Language) => void;
}) {
  const { t } = useTranslation();
  const next: Language = language === 'en' ? 'es' : 'en';

  return (
    <button
      type="button"
      onClick={() => setLanguage(next)}
      aria-label={t('language.switchTo', { language: LANGUAGE_NAMES[next] })}
      className="flex h-8 items-center gap-1 rounded-lg px-1.5 text-muted transition hover:bg-edge hover:text-body"
    >
      <Globe className="h-4 w-4" aria-hidden />
      <span className="text-sm font-semibold uppercase">{language}</span>
    </button>
  );
}
