import { describe, expect, it } from 'vitest';
import en from './locales/en.json';
import es from './locales/es.json';
import { SUPPORTED_LANGUAGES } from './language';

/**
 * The guard that keeps the string sweeps honest (M43). Vitest here runs `environment: 'node'` over
 * `.test.ts` files only — no Testing Library, no `.tsx` — so this is a pure comparison of the two
 * resource files. It catches the realistic failure by far: a key added to `en` and forgotten in `es`.
 *
 * `en.json` is the shape of record (the `t()` types are generated from it in `i18next.d.ts`); every
 * other language is measured against it.
 */

type Json = { [key: string]: string | Json };

/** `{ nav: { home: 'Home' } }` → `{ 'nav.home': 'Home' }`. */
function flatten(node: Json, prefix = ''): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [key, value] of Object.entries(node)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (typeof value === 'string') out[path] = value;
    else Object.assign(out, flatten(value, path));
  }
  return out;
}

/** The `{{name}}` placeholders in a string, sorted — `{{count}}` included. */
function placeholders(value: string): string[] {
  return [...value.matchAll(/\{\{\s*([\w.]+)\s*[^}]*\}\}/g)].map((m) => m[1]).sort();
}

const LOCALES: Record<string, Record<string, string>> = {
  en: flatten(en as Json),
  es: flatten(es as Json),
};

const PLURAL_SUFFIXES = ['_one', '_other'] as const;

describe('translation resources', () => {
  it('ships a resource file for every supported language', () => {
    expect(Object.keys(LOCALES).sort()).toEqual([...SUPPORTED_LANGUAGES].sort());
  });

  describe.each(Object.keys(LOCALES).filter((l) => l !== 'en'))('%s', (locale) => {
    const translated = LOCALES[locale];

    it('has exactly the same keys as en', () => {
      const missing = Object.keys(LOCALES.en).filter((k) => !(k in translated));
      const extra = Object.keys(translated).filter((k) => !(k in LOCALES.en));
      expect({ missing, extra }).toEqual({ missing: [], extra: [] });
    });

    it('interpolates the same placeholders as en for every key', () => {
      const mismatched = Object.keys(LOCALES.en)
        .filter((k) => k in translated)
        .filter((k) => placeholders(LOCALES.en[k]).join() !== placeholders(translated[k]).join());
      expect(mismatched).toEqual([]);
    });
  });

  describe.each(Object.keys(LOCALES))('%s', (locale) => {
    const values = LOCALES[locale];

    it('has no blank values', () => {
      expect(Object.keys(values).filter((k) => values[k].trim() === '')).toEqual([]);
    });

    it('has a complete _one/_other family for every plural key', () => {
      const incomplete = Object.keys(values)
        .filter((k) => PLURAL_SUFFIXES.some((s) => k.endsWith(s)))
        .filter((k) => {
          const base = k.replace(/_(one|other)$/, '');
          return PLURAL_SUFFIXES.some((s) => !(base + s in values));
        });
      expect(incomplete).toEqual([]);
    });

    it('never pairs a plural family with a same-named singular key', () => {
      // `days` alongside `days_one` is ambiguous to i18next — the count-less lookup wins silently.
      const shadowed = Object.keys(values)
        .filter((k) => k.endsWith('_one'))
        .map((k) => k.replace(/_one$/, ''))
        .filter((base) => base in values);
      expect(shadowed).toEqual([]);
    });
  });
});
