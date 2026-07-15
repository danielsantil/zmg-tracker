import type { PendingKind } from './enums';

// An action is owned by either a release (releaseId) or a song (songId); subject is that owner's
// display name (release title / song title).
export interface PendingAction {
  kind: PendingKind;
  label: string;
  subject: string;
  artistName: string;
  releaseId: string | null;
  songId: string | null;
  taskId: string | null;
  daysToRelease: number | null;
}
