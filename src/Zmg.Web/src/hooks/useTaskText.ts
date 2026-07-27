import { useCallback } from 'react';
import { useLanguage } from '@/i18n/useLanguage';
import { taskText } from '@/lib/taskText';
import type { TaskText } from '@/types';

/**
 * `taskText` bound to the language the user is currently reading. A hook because it must re-render on
 * a language switch — and re-rendering is *all* it takes, since both columns are already in hand.
 */
export function useTaskText(): (task: TaskText) => string {
  const { language } = useLanguage();
  return useCallback((task: TaskText) => taskText(language, task), [language]);
}
