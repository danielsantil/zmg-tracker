import type { Phase } from './enums';

/**
 * Checklist task text is the one place in the app that carries two languages (v2.9). Everything else
 * — song titles, artist names, release titles, notes — stays single-value, deliberately: this is a
 * checklist feature, not an app-wide pattern.
 *
 * `titleEs` is nullable, and null is a legitimate answer rather than a missing translation: it means
 * "show the English to Spanish readers too", which is right for a proper noun or for a task the user
 * only worded once. Resolve it through `lib/taskText.ts`, never by hand.
 */
export interface TaskText {
  titleEn: string;
  titleEs: string | null;
}

/** Add-a-task payload — identical for release tasks and template tasks. */
export interface TaskAddInput extends TaskText {
  phase: Phase;
  minDaysBefore?: number | null;
  maxDaysBefore?: number | null;
}

/** Reorder-a-phase payload — the full ordered id list for one phase (release + template). */
export interface PhaseReorderInput {
  phase: Phase;
  orderedTaskIds: string[];
}

/** Update payload for a template task (no notes). Release tasks extend it with `notes`. */
export interface TemplateTaskUpdateInput extends TaskText {
  phase: Phase;
  minDaysBefore: number | null;
  maxDaysBefore: number | null;
}

/** Update payload for a release task — a template-task update plus notes. */
export interface ReleaseTaskUpdateInput extends TemplateTaskUpdateInput {
  notes: string | null;
}

export interface ReleaseTaskDto extends TaskText {
  id: string;
  phase: Phase;
  sortOrder: number;
  isDone: boolean;
  completedAt: string | null;
  notes: string | null;
  minDaysBefore: number | null;
  maxDaysBefore: number | null;
}

export interface PhaseGroup {
  phase: Phase;
  done: number;
  total: number;
  tasks: ReleaseTaskDto[];
}
