import type { Phase } from '@/types';

/** The full editable state of a checklist task — everything `TaskEditorModal` owns, in one shape. */
export interface TaskDraft {
  titleEn: string;
  /** Empty string is the "no Spanish" state; the API stores it as null. */
  titleEs: string;
  phase: Phase;
  minDaysBefore: number | null;
  maxDaysBefore: number | null;
  /** Release tasks only; templates ignore it. */
  notes: string;
}

export const emptyDraft = (phase: Phase): TaskDraft => ({
  titleEn: '',
  titleEs: '',
  phase,
  minDaysBefore: null,
  maxDaysBefore: null,
  notes: '',
});
