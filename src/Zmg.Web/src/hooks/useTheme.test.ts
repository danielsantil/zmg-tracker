import { afterEach, describe, expect, it, vi } from 'vitest';
import { isTheme, resolveInitialTheme } from './useTheme';

/** Stub the global `matchMedia` (absent in the node test env) so the dark query returns `matches`. */
function mockPrefersDark(matches: boolean) {
  vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches } as MediaQueryList));
}

afterEach(() => {
  localStorage.clear();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe('isTheme', () => {
  it('accepts the two theme literals and rejects anything else', () => {
    expect(isTheme('light')).toBe(true);
    expect(isTheme('dark')).toBe(true);
    expect(isTheme('system')).toBe(false);
    expect(isTheme(null)).toBe(false);
    expect(isTheme(1)).toBe(false);
  });
});

describe('resolveInitialTheme', () => {
  it('uses a saved choice over the OS preference', () => {
    // Arrange — saved light, but the OS asks for dark.
    localStorage.setItem('zmg.theme', JSON.stringify('light'));
    mockPrefersDark(true);

    // Act / Assert
    expect(resolveInitialTheme()).toBe('light');
  });

  it('follows the OS when no choice is saved', () => {
    // Arrange
    mockPrefersDark(true);

    // Act / Assert
    expect(resolveInitialTheme()).toBe('dark');

    // Arrange — flip the OS preference.
    mockPrefersDark(false);
    expect(resolveInitialTheme()).toBe('light');
  });

  it('ignores a stale/invalid saved value and follows the OS', () => {
    // Arrange — a value the guard rejects (e.g. an old "system" build).
    localStorage.setItem('zmg.theme', JSON.stringify('system'));
    mockPrefersDark(false);

    // Act / Assert
    expect(resolveInitialTheme()).toBe('light');
  });

  it('defaults to dark when matchMedia is unavailable', () => {
    // Arrange — the node env has no matchMedia and nothing is saved; the guarded call falls to dark.
    expect(globalThis.matchMedia).toBeUndefined();

    // Act / Assert
    expect(resolveInitialTheme()).toBe('dark');
  });
});
