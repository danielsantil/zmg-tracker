import { afterEach, describe, expect, it, vi } from 'vitest';
import { LANGUAGE_KEY, browserLanguage, isLanguage, resolveInitialLanguage } from './language';

/** Point `navigator.language` at a value without depending on the runner's own locale. */
function stubNavigator(language: string | null) {
  vi.stubGlobal('navigator', language === null ? undefined : { language });
}

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
  localStorage.clear();
});

describe('isLanguage', () => {
  it('accepts only the shipped codes', () => {
    expect(isLanguage('en')).toBe(true);
    expect(isLanguage('es')).toBe(true);
    expect(isLanguage('es-MX')).toBe(false);
    expect(isLanguage('fr')).toBe(false);
    expect(isLanguage(null)).toBe(false);
  });
});

describe('browserLanguage', () => {
  it('narrows a regional tag to its primary subtag', () => {
    stubNavigator('es-MX');
    expect(browserLanguage()).toBe('es');
  });

  it('falls back to English for a language we do not ship', () => {
    stubNavigator('pt-BR');
    expect(browserLanguage()).toBe('en');
  });

  it('falls back to English where navigator is unavailable', () => {
    stubNavigator(null);
    expect(browserLanguage()).toBe('en');
  });
});

describe('resolveInitialLanguage', () => {
  it('prefers a saved choice over the browser', () => {
    stubNavigator('en-US');
    localStorage.setItem(LANGUAGE_KEY, JSON.stringify('es'));
    expect(resolveInitialLanguage()).toBe('es');
  });

  it('follows the browser when nothing is saved', () => {
    stubNavigator('es-ES');
    expect(resolveInitialLanguage()).toBe('es');
  });

  it('ignores a stored value the app can no longer render', () => {
    stubNavigator('en-US');
    localStorage.setItem(LANGUAGE_KEY, JSON.stringify('fr'));
    expect(resolveInitialLanguage()).toBe('en');
  });

  it('falls back to the browser when localStorage throws', () => {
    stubNavigator('es');
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    expect(resolveInitialLanguage()).toBe('es');
  });
});
