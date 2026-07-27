import type { Phase, ReleaseType } from './enums';
import type { TaskText } from './task';

// Templates (M3 template management)
export interface TemplateTaskDto extends TaskText {
  id: string;
  phase: Phase;
  sortOrder: number;
  minDaysBefore: number | null;
  maxDaysBefore: number | null;
}

export interface TemplatePhaseGroup {
  phase: Phase;
  tasks: TemplateTaskDto[];
}

export interface Template {
  id: string;
  type: ReleaseType;
  phases: TemplatePhaseGroup[];
}
