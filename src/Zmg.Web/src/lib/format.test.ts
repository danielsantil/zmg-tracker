import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { countdown, daysToRelease, formatReleaseDate, timeframeHint, todayIso } from './format';

describe('timeframeHint', () => {
  it('returns null when neither bound is set', () => {
    expect(timeframeHint(null, null)).toBeNull();
  });

  it('collapses an equal min/max to a single count', () => {
    expect(timeframeHint(7, 7)).toEqual({ kind: 'single', count: 7 });
  });

  it('reports a range when min and max differ', () => {
    expect(timeframeHint(7, 14)).toEqual({ kind: 'range', min: 7, max: 14 });
  });

  it('falls back to the set bound when only one is present', () => {
    expect(timeframeHint(null, 14)).toEqual({ kind: 'single', count: 14 });
    expect(timeframeHint(7, null)).toEqual({ kind: 'single', count: 7 });
  });
});

describe('formatReleaseDate', () => {
  it('renders the date in the given locale, read off the string rather than UTC', () => {
    // The 1st is the case a UTC parse would drift back into the previous month.
    expect(formatReleaseDate('2026-08-01', 'en')).toBe('Aug 1, 2026');
    expect(formatReleaseDate('2026-08-01', 'es')).toMatch(/2026/);
    expect(formatReleaseDate('2026-08-01', 'es')).toMatch(/^1 /);
  });
});

describe('date-relative helpers', () => {
  beforeEach(() => {
    // A fixed *local* instant late in the day — the case that exposes the UTC/local split.
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 6, 17, 23, 0, 0));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('todayIso', () => {
    it('reports the local calendar date regardless of the runner timezone', () => {
      // Built from local parts, so 23:00 local on the 17th is always the 17th — never the UTC
      // rollover that `toISOString()` would report in a negative offset.
      expect(todayIso()).toBe('2026-07-17');
    });
  });

  describe('daysToRelease', () => {
    it('counts whole days from local midnight to the release date', () => {
      expect(daysToRelease('2026-07-20')).toBe(3);
    });

    it('is 0 on the release day and negative once past', () => {
      expect(daysToRelease('2026-07-17')).toBe(0);
      expect(daysToRelease('2026-07-16')).toBe(-1);
    });
  });

  describe('countdown', () => {
    it('reports "today" on the day', () => {
      expect(countdown('2026-07-17')).toBe('today');
    });

    it('counts the days up for a future upcoming release', () => {
      expect(countdown('2026-07-20')).toEqual({ days: 3 });
    });

    it('returns null once the release date has passed', () => {
      expect(countdown('2026-07-16')).toBeNull();
    });
  });
});
