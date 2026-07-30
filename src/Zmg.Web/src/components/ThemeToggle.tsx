import { useTranslation } from 'react-i18next';
import { Moon, Sun } from 'lucide-react';
import type { Theme } from '@/hooks/useTheme';

/**
 * Theme switch button — shows the mode you'd switch TO: a sun in dark mode (→ light), a moon in light
 * (→ dark). Presentational like `LanguageToggle`: the page owns the single `useTheme` and passes it in,
 * because `useTheme` is per-caller state and two independent calls would diverge on toggle.
 */
export function ThemeToggle({ theme, toggle }: { theme: Theme; toggle: () => void }) {
  const { t } = useTranslation();
  const Icon = theme === 'dark' ? Sun : Moon;
  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={theme === 'dark' ? t('theme.toLight') : t('theme.toDark')}
      className="grid h-8 w-8 place-items-center rounded-lg text-muted transition hover:bg-edge hover:text-body"
    >
      <Icon className="h-4 w-4" aria-hidden />
    </button>
  );
}
