import { afterEach, describe, expect, it, vi } from 'vitest';
import { persistKey, readPersisted, writePersisted } from './usePersistedState';

const isString = (v: unknown): v is string => typeof v === 'string';

afterEach(() => {
  localStorage.clear();
  vi.restoreAllMocks();
});

describe('persistKey', () => {
  it('namespaces the key under zmg.', () => {
    expect(persistKey('releasesView')).toBe('zmg.releasesView');
  });
});

describe('readPersisted', () => {
  it('returns the initial value when the key is absent', () => {
    // Act
    const value = readPersisted('zmg.view', 'table', isString);

    // Assert
    expect(value).toBe('table');
  });

  it('returns the stored value when it passes the guard', () => {
    // Arrange
    localStorage.setItem('zmg.view', JSON.stringify('calendar'));

    // Act
    const value = readPersisted('zmg.view', 'table', isString);

    // Assert
    expect(value).toBe('calendar');
  });

  it('rejects a stale value the guard fails and falls back to initial', () => {
    // Arrange — a key left by an older build that stored a number where a string is expected.
    localStorage.setItem('zmg.view', JSON.stringify(42));

    // Act
    const value = readPersisted('zmg.view', 'table', isString);

    // Assert
    expect(value).toBe('table');
  });

  it('falls back to initial on unparseable JSON', () => {
    // Arrange
    localStorage.setItem('zmg.view', '{not json');

    // Act
    const value = readPersisted('zmg.view', 'table', isString);

    // Assert
    expect(value).toBe('table');
  });

  it('falls back to initial when localStorage throws (private mode / blocked)', () => {
    // Arrange
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('access denied');
    });

    // Act
    const value = readPersisted('zmg.view', 'table', isString);

    // Assert
    expect(value).toBe('table');
  });
});

describe('writePersisted', () => {
  it('serializes the value to localStorage', () => {
    // Act
    writePersisted('zmg.view', 'calendar');

    // Assert
    expect(localStorage.getItem('zmg.view')).toBe(JSON.stringify('calendar'));
  });

  it('swallows a throwing localStorage instead of taking the page down', () => {
    // Arrange
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('quota exceeded');
    });

    // Act / Assert — no throw escapes.
    expect(() => writePersisted('zmg.view', 'calendar')).not.toThrow();
  });
});
