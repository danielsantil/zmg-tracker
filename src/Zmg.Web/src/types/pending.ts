import type { PendingKind } from './enums';

// An action is owned by either a release (releaseId) or a song (songId); subject is that owner's
// display name (release title / song title).
export interface PendingAction {
  kind: PendingKind;
  /**
   * The advisory's i18next code, for the three data kinds — run it through `useServerText()`.
   * Null on `TaskDue`, which carries the task's own text instead (v2.9).
   */
  warningCode: string | null;
  /**
   * `TaskDue` only: the task's own text, user content rendered verbatim, in both languages so the
   * row follows a language switch without a refetch. Null on the data kinds.
   */
  taskTitleEn: string | null;
  taskTitleEs: string | null;
  subject: string;
  artistName: string;
  releaseId: string | null;
  songId: string | null;
  taskId: string | null;
  daysToRelease: number | null;
}
