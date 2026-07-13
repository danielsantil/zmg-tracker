import type { PendingKind } from './enums';

export interface PendingAction {
  releaseId: string;
  releaseTitle: string;
  artistName: string;
  kind: PendingKind;
  taskId: string | null;
  label: string;
  daysToRelease: number | null;
}
