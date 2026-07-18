import type { Phase } from './enums';

/** Add-a-task payload — identical for release tasks and template tasks. */
export interface TaskAddInput {
  title: string;
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
export interface TemplateTaskUpdateInput {
  title: string;
  phase: Phase;
  minDaysBefore: number | null;
  maxDaysBefore: number | null;
}

/** Update payload for a release task — a template-task update plus notes. */
export interface ReleaseTaskUpdateInput extends TemplateTaskUpdateInput {
  notes: string | null;
}

export interface ReleaseTaskDto {
  id: string;
  title: string;
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
