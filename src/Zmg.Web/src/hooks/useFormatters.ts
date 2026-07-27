import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { countdown, formatReleaseDate, timeframeHint } from '@/lib/format';
import { monthLabel, type YearMonth } from '@/lib/calendar';

/**
 * The bridge between `lib/format`'s pure shapes and the words for them (M43). Those helpers
 * deliberately return *what* to say (`{ days: 3 }`, `{ kind: 'range', … }`) rather than a sentence,
 * because plural forms and word order differ per language — this hook is where the two meet, so no
 * component has to remember which key spells a countdown.
 *
 * Memoized on the active language: every returned function closes over it, and they're used inside
 * render, not effects.
 */
export function useFormatters() {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  return useMemo(
    () => ({
      /** "Aug 22, 2026" / "22 ago 2026". */
      releaseDate: (date: string) => formatReleaseDate(date, locale),

      /** "August 2026" / "agosto de 2026" for the calendar header. */
      month: (ym: YearMonth) => monthLabel(ym, locale),

      /** "in 3 days" / "Releasing today", or null once the release date has passed. */
      countdown: (releaseDate: string): string | null => {
        const value = countdown(releaseDate);
        if (value === null) return null;
        return value === 'today' ? t('countdown.today') : t('countdown.days', { count: value.days });
      },

      /** "7–14 days before", or null when the task carries no timeframe. */
      timeframe: (min: number | null, max: number | null): string | null => {
        const hint = timeframeHint(min, max);
        if (hint === null) return null;
        return hint.kind === 'single'
          ? t('timeframe.days', { count: hint.count })
          : t('timeframe.range', { min: hint.min, max: hint.max });
      },
    }),
    [t, locale],
  );
}
