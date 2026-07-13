import type { Phase, ReleaseType } from './enums';

// Templates (M3 template management)
export interface TemplateTaskDto {
  id: string;
  title: string;
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
