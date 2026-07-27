/**
 * What a Pre task's timeframe should render as, or null when no timeframe is set: either a single
 * count ("7 days before") or a range ("7–14 days before"). Max drives calculations; the range is
 * display-only (v1.1 M8).
 *
 * Returns the *shape*, not the sentence — the words are i18next keys (`timeframe.*`), because
 * Spanish doesn't put them in the same order (M43). Pure, so it stays testable here.
 */
export type TimeframeHint =
  | { kind: 'single'; count: number }
  | { kind: 'range'; min: number; max: number };

export function timeframeHint(min: number | null, max: number | null): TimeframeHint | null {
  if (min == null && max == null) return null;
  if (min != null && max != null) {
    return min === max ? { kind: 'single', count: max } : { kind: 'range', min, max };
  }
  return { kind: 'single', count: (max ?? min)! };
}

/**
 * Today as a yyyy-MM-dd string, for lexicographic comparison against release dates. Built from
 * *local* parts, never `toISOString()` (which is UTC and returns tomorrow's date in a negative
 * offset after 00:00 UTC — same rule as `formatReleaseDate`/`lib/calendar`).
 */
export function todayIso(): string {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

export function daysToRelease(date: string): number {
  const d = new Date(date + 'T00:00:00');
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  return Math.round((d.getTime() - now.getTime()) / 86_400_000);
}

/**
 * A release date as "Aug 22, 2026" (`en`) / "22 ago 2026" (`es`) — `Intl` owns the word order, so
 * callers pass `i18n.language` and nothing here needs to know the format. Parsed as local midnight
 * (never `new Date('yyyy-MM-dd')`, which is UTC and drifts a day back in negative offsets).
 */
export function formatReleaseDate(date: string, locale = 'en'): string {
  return new Date(date + 'T00:00:00').toLocaleDateString(locale, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

/**
 * How far off an upcoming release is: `{ days }` to count down, `'today'` on the day, or null once
 * it's released. Like `timeframeHint`, this returns the shape and the component supplies the words
 * (`countdown.*`) — the sentence is a plural form, which differs per language.
 */
export type Countdown = 'today' | { days: number };

export function countdown(releaseDate: string): Countdown | null {
  const days = daysToRelease(releaseDate);
  if (days < 0) return null;
  return days === 0 ? 'today' : { days };
}
