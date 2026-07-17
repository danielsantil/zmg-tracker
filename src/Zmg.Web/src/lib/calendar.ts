/**
 * Month-grid helpers for the releases calendar (M22). Every cell is a `yyyy-MM-dd` string built
 * by hand so it can be compared lexicographically against a release's raw `releaseDate` — the
 * grid never parses dates back out of `new Date('yyyy-MM-dd')`, which is UTC and drifts a day
 * back in negative offsets (same rule as `formatReleaseDate`/`todayIso`).
 */

const pad = (n: number) => String(n).padStart(2, '0');

/** `yyyy-MM-dd` for a y/m/d triple, with `month` 0-indexed like `Date`. */
export function toIso(year: number, month: number, day: number): string {
  return `${year}-${pad(month + 1)}-${pad(day)}`;
}

export interface YearMonth {
  year: number;
  /** 0-indexed, like `Date.getMonth()`. */
  month: number;
}

/** The month containing `iso` (a `yyyy-MM-dd` string), read off the string rather than parsed. */
export function monthOf(iso: string): YearMonth {
  return { year: Number(iso.slice(0, 4)), month: Number(iso.slice(5, 7)) - 1 };
}

/** Shift a month by `delta` months, rolling the year over in either direction. */
export function addMonths({ year, month }: YearMonth, delta: number): YearMonth {
  const total = year * 12 + month + delta;
  return { year: Math.floor(total / 12), month: ((total % 12) + 12) % 12 };
}

/** "August 2026" for the calendar header. */
export function monthLabel({ year, month }: YearMonth): string {
  return new Date(year, month, 1).toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
}

/**
 * A fixed 6×7 grid of `yyyy-MM-dd` cells for the given month, Sunday-first, padded with the
 * adjacent months' days. Fixed height keeps the grid from reflowing as you page through months.
 */
export function monthGrid({ year, month }: YearMonth): string[][] {
  const firstWeekday = new Date(year, month, 1).getDay();
  const weeks: string[][] = [];
  // `new Date(y, m, 0)` is the last day of the previous month, so day 1 minus the weekday offset
  // walks back into it; Date normalizes the overflow in both directions for us.
  const start = new Date(year, month, 1 - firstWeekday);
  for (let w = 0; w < 6; w++) {
    const week: string[] = [];
    for (let d = 0; d < 7; d++) {
      const cell = new Date(start.getFullYear(), start.getMonth(), start.getDate() + w * 7 + d);
      week.push(toIso(cell.getFullYear(), cell.getMonth(), cell.getDate()));
    }
    weeks.push(week);
  }
  return weeks;
}

/** Whether `iso` falls inside `ym` — a string compare on the `yyyy-MM` prefix. */
export function isInMonth(iso: string, { year, month }: YearMonth): boolean {
  return iso.slice(0, 7) === `${year}-${pad(month + 1)}`;
}

export const WEEKDAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
