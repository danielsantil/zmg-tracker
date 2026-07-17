import { describe, expect, it } from 'vitest';
import { addMonths, isInMonth, monthGrid, monthOf, toIso } from './calendar';

describe('toIso', () => {
  it('zero-pads month and day and 1-indexes the month', () => {
    // Arrange
    const [year, month, day] = [2026, 6, 3]; // month is 0-indexed like Date

    // Act
    const iso = toIso(year, month, day);

    // Assert
    expect(iso).toBe('2026-07-03');
  });
});

describe('monthOf', () => {
  it('reads the year/month off the string without parsing a Date', () => {
    // Act
    const ym = monthOf('2026-07-15');

    // Assert — month comes back 0-indexed
    expect(ym).toEqual({ year: 2026, month: 6 });
  });
});

describe('addMonths', () => {
  it('rolls the year forward across December', () => {
    // Act
    const ym = addMonths({ year: 2026, month: 11 }, 1); // Dec 2026 + 1

    // Assert
    expect(ym).toEqual({ year: 2027, month: 0 });
  });

  it('rolls the year backward across January', () => {
    // Act
    const ym = addMonths({ year: 2026, month: 0 }, -1); // Jan 2026 - 1

    // Assert
    expect(ym).toEqual({ year: 2025, month: 11 });
  });
});

describe('monthGrid', () => {
  it('emits exactly 4 weeks when the month starts Sunday and fits (Feb 2026)', () => {
    // Arrange — Feb 2026 starts on a Sunday and has 28 days: a perfect 4×7 with no foreign days.
    const grid = monthGrid({ year: 2026, month: 1 });

    // Assert
    expect(grid).toHaveLength(4);
    expect(grid[0][0]).toBe('2026-02-01');
    expect(grid[3][6]).toBe('2026-02-28');
  });

  it('trims the trailing all-foreign week (Jul 2026 → 5 weeks, not 6)', () => {
    // Arrange — Wed start + 31 days = 34 cells; a fixed-6 grid would tack on an all-August week.
    const grid = monthGrid({ year: 2026, month: 6 });

    // Assert
    expect(grid).toHaveLength(5);
    // Leading fill walks back into June via the `new Date(y, m, 1 - firstWeekday)` overflow trick.
    expect(grid[0][0]).toBe('2026-06-28');
    // Trailing fill spills one day into August — the last week still holds days of July.
    expect(grid[4][6]).toBe('2026-08-01');
  });

  it('uses all 6 weeks when the month needs them (Aug 2026)', () => {
    // Arrange — Sat start + 31 days genuinely spans 6 weeks.
    const grid = monthGrid({ year: 2026, month: 7 });

    // Assert
    expect(grid).toHaveLength(6);
    expect(grid[0][0]).toBe('2026-07-26');
    expect(grid[5][6]).toBe('2026-09-05');
  });

  it('produces rows of 7 and includes the first of the month exactly once', () => {
    // Act
    const grid = monthGrid({ year: 2026, month: 2 }); // Mar 2026
    const flat = grid.flat();

    // Assert
    expect(grid.every((week) => week.length === 7)).toBe(true);
    expect(flat.filter((iso) => iso === '2026-03-01')).toHaveLength(1);
  });
});

describe('isInMonth', () => {
  it('matches a date inside the month and rejects the adjacent-month fill', () => {
    // Arrange
    const august = { year: 2026, month: 7 };

    // Assert
    expect(isInMonth('2026-08-15', august)).toBe(true);
    expect(isInMonth('2026-07-31', august)).toBe(false);
    expect(isInMonth('2026-09-01', august)).toBe(false);
  });
});
