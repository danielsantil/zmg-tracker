import { describe, expect, it } from 'vitest';
import { taskText } from './taskText';

/**
 * The client-side half of v2.9: the API ships both columns and never picks a language, so this
 * function is the only thing standing between a task row and a blank line. Its contract is "always
 * return something a human wrote", and every case below is one way that could fail.
 */
describe('taskText', () => {
  it('returns the English text to an English reader', () => {
    expect(taskText('en', { titleEn: 'Mix/master', titleEs: 'Mezcla/master' })).toBe('Mix/master');
  });

  it('returns the Spanish text to a Spanish reader', () => {
    expect(taskText('es', { titleEn: 'Mix/master', titleEs: 'Mezcla/master' })).toBe('Mezcla/master');
  });

  it('never shows Spanish to an English reader, even when it is the only text worth reading', () => {
    expect(taskText('en', { titleEn: 'Mix/master', titleEs: 'Mezcla/master' })).not.toBe('Mezcla/master');
  });

  describe('falls back to English rather than rendering nothing', () => {
    // Null is the deliberate "reads the same in both languages" state, not a missing translation —
    // and blank/whitespace are what a user leaving the field alone actually produces.
    it.each([
      ['null', null],
      ['empty', ''],
      ['whitespace', '   '],
    ])('when the Spanish is %s', (_label, titleEs) => {
      expect(taskText('es', { titleEn: 'Spotify Canvas', titleEs })).toBe('Spotify Canvas');
    });
  });

  it('does not trim the text it returns', () => {
    // Trimming belongs to the editor on the way in; doing it again on the way out would quietly
    // disagree with what is stored and make round-trip comparisons lie.
    expect(taskText('es', { titleEn: 'en', titleEs: ' Mezcla ' })).toBe(' Mezcla ');
  });
});
