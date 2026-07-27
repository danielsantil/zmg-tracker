import { useTranslation } from 'react-i18next';
import { Languages } from 'lucide-react';
import { LANGUAGE_NAMES, type Language } from '@/i18n/language';

/**
 * The UI language switcher (deferred from M37, landed in M43). Shows the language you'd switch **TO**,
 * matching `ThemeToggle`'s convention — with exactly two languages a toggle beats a dropdown. The
 * target language's own name is used for the label (`Español`, not `Spanish`), which is the
 * accessible convention for a language switcher.
 *
 * Presentational: state comes from NavBar's single `useLanguage`, like `ThemeToggle`'s theme.
 * If a third language ever lands this becomes a popover — which must portal to `<body>` per the
 * standing rule, since it would sit inside a transform-free but stacking-context'd header.
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
      <Languages className="h-4 w-4" aria-hidden />
      <span className="text-xs font-semibold uppercase">{next}</span>
    </button>
  );
}
