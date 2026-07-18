import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { daysToRelease, formatCountdown, formatTimeframe, todayIso } from './format';

describe('formatTimeframe', () => {
  it('returns null when neither bound is set', () => {
    expect(formatTimeframe(null, null)).toBeNull();
  });

  it('collapses an equal min/max to a single value', () => {
    expect(formatTimeframe(7, 7)).toBe('7 days before');
  });

  it('renders a range when min and max differ', () => {
    expect(formatTimeframe(7, 14)).toBe('7–14 days before');
  });

  it('falls back to the set bound when only one is present', () => {
    expect(formatTimeframe(null, 14)).toBe('14 days before');
    expect(formatTimeframe(7, null)).toBe('7 days before');
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

  describe('formatCountdown', () => {
    it('says "Releasing today" on the day', () => {
      expect(formatCountdown('2026-07-17')).toBe('Releasing today');
    });

    it('counts the days up for a future upcoming release', () => {
      expect(formatCountdown('2026-07-20')).toBe('in 3 days');
    });

    it('returns null once the release date has passed', () => {
      expect(formatCountdown('2026-07-16')).toBeNull();
    });
  });
});
