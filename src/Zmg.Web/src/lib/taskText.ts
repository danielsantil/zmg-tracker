import type { Language } from '@/i18n/language';
import type { TaskText } from '@/types';

/**
 * The one place checklist text picks a language (v2.9). The API ships both columns and never resolves
 * a locale, so this runs client-side — which is what makes switching language a re-render rather than
 * a refetch, and why there is no cache to invalidate and no request ordering to get wrong.
 *
 * A blank or absent Spanish falls back to English. That is a legitimate state, not a gap: proper nouns
 * and one-worded user tasks live there, and it must never render as an empty row.
 *
 * Pure and language-explicit rather than a hook, so non-component code can call it; components should
 * reach for `useTaskText()` below, which binds the current language for them.
 */
export function taskText(language: Language, task: TaskText): string {
  return language === 'es' && task.titleEs?.trim() ? task.titleEs : task.titleEn;
}
