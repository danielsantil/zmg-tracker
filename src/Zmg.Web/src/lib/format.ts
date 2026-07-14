/**
 * Human timeframe hint for a Pre task ("7–14 days before"), or null when no timeframe is set.
 * Max drives calculations; the range is display-only (v1.1 M8).
 */
export function formatTimeframe(min: number | null, max: number | null): string | null {
  if (min == null && max == null) return null;
  if (min != null && max != null) {
    return min === max ? `${max} days before` : `${min}–${max} days before`;
  }
  return `${max ?? min} days before`;
}

/** Today as a yyyy-MM-dd string, for lexicographic comparison against release dates. */
export function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function daysToRelease(date: string): number {
  const d = new Date(date + 'T00:00:00');
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  return Math.round((d.getTime() - now.getTime()) / 86_400_000);
}

/**
 * Countdown string for an upcoming release ("3 days to release" / "Releases today"),
 * or null once it's released or not upcoming. Shared by release cards and the detail header.
 */
export function formatCountdown(status: string, releaseDate: string): string | null {
  const days = daysToRelease(releaseDate);
  if (status !== 'Upcoming' || days < 0) return null;
  return days === 0 ? 'Releases today' : `${days} days to release`;
}
