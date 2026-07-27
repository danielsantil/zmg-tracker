import { useTranslation } from 'react-i18next';
import { MenuItem, ReorderArrows, RowMenu } from '@/components';
import { useFormatters } from '@/hooks/useFormatters';
import { useTaskText } from '@/hooks/useTaskText';
import type { Phase, TaskText } from '@/types';

/** The fields a checklist row needs, shared by release tasks and template tasks (M24.5). */
export interface ChecklistTask extends TaskText {
  id: string;
  phase: Phase;
  minDaysBefore: number | null;
  maxDaysBefore: number | null;
}

/**
 * One checklist row, generic over release tasks and template tasks. The release-only affordances are
 * opt-in: pass `onToggle` (+ `isDone`) to get the done checkbox and a click-to-toggle title, and
 * `notes` to get the notes indicator and preview. Without them the row is the template shape.
 *
 * The row itself edits nothing (v2.9) — `onEdit` opens `TaskEditorModal`, which owns every editable
 * field. Inline rename used to live here, and it could only ever offer *one* text box for what is now
 * two columns.
 */
export function TaskRow<T extends ChecklistTask>({
  task,
  isFirst,
  isLast,
  readOnly = false,
  onToggle,
  isDone = false,
  supportsNotes = false,
  notes = null,
  onEdit,
  onDelete,
  onMove,
}: {
  task: T;
  isFirst: boolean;
  isLast: boolean;
  readOnly?: boolean;
  onToggle?: (t: T) => void;
  isDone?: boolean;
  supportsNotes?: boolean;
  notes?: string | null;
  onEdit: (t: T) => void;
  onDelete: (t: T) => void;
  onMove: (t: T, dir: -1 | 1) => void;
}) {
  const { t } = useTranslation();
  const text = useTaskText();

  const timeframe = useFormatters().timeframe(task.minDaysBefore, task.maxDaysBefore);
  const title = text(task);

  return (
    <li className="border-b border-edge/50 last:border-b-0">
      <div className="flex items-center gap-3 px-4 py-2.5">
        {onToggle && (
          <button
            role="checkbox"
            aria-checked={isDone}
            aria-label={title}
            disabled={readOnly}
            onClick={() => !readOnly && onToggle(task)}
            className={`grid h-6 w-6 shrink-0 place-items-center rounded-md border transition ${
              isDone
                ? 'border-accent bg-accent text-white'
                : 'border-edge bg-panel hover:border-accent'
            } ${readOnly ? 'cursor-default opacity-70 hover:border-edge' : ''}`}
          >
            {isDone && '✓'}
          </button>
        )}

        <button
          className={`flex-1 text-left text-sm ${isDone ? 'text-subtle line-through' : 'text-strong'} ${readOnly ? 'cursor-default' : ''}`}
          disabled={readOnly}
          onClick={() => {
            if (readOnly) return;
            // Release rows toggle done on title click; template rows (no onToggle) open the editor.
            if (onToggle) onToggle(task);
            else onEdit(task);
          }}
        >
          {title}
          {timeframe && (
            <span className="ml-2 whitespace-nowrap text-xs text-accent/80">· {timeframe}</span>
          )}
          {supportsNotes && notes && (
            <span className="ml-1.5 text-xs text-subtle" title={t('tasks.hasNotes')} aria-label={t('tasks.hasNotes')}>
              ✎
            </span>
          )}
        </button>

        {!readOnly && (
          <div className="flex shrink-0 items-center">
            <ReorderArrows isFirst={isFirst} isLast={isLast} onMove={(dir) => onMove(task, dir)} />
            <div className="ml-3">
              <RowMenu>
                {(close) => (
                  <>
                    <MenuItem onClick={() => { close(); onEdit(task); }}>{t('common.edit')}</MenuItem>
                    <MenuItem tone="danger" onClick={() => { close(); onDelete(task); }}>
                      {t('common.delete')}
                    </MenuItem>
                  </>
                )}
              </RowMenu>
            </div>
          </div>
        )}
      </div>

      {supportsNotes && notes && <p className="px-4 pb-2.5 pl-12 text-xs text-muted">{notes}</p>}
    </li>
  );
}
