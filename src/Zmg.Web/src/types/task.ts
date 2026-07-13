import type { Phase } from './enums';

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
