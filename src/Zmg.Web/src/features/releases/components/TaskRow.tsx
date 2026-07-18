import { useState } from 'react';
import { Phase } from '@/types';
import { MenuItem, ReorderArrows, RowMenu, inputClass } from '@/components';
import { formatTimeframe } from '@/lib/format';
import { TimeframeEditor } from './TimeframeEditor';
import { MovePhaseItems } from './MovePhaseItems';

/** The fields a checklist row needs, shared by release tasks and template tasks (M24.5). */
export interface ChecklistTask {
  id: string;
  title: string;
  phase: Phase;
  minDaysBefore: number | null;
  maxDaysBefore: number | null;
}

export type TaskPatch = Partial<Pick<ChecklistTask, 'title' | 'phase' | 'minDaysBefore' | 'maxDaysBefore'>> & {
  notes?: string | null;
};

/**
 * One checklist row, generic over release tasks and template tasks. The release-only affordances are
 * opt-in: pass `onToggle` (+ `isDone`) to get the done checkbox and a click-to-toggle title, and
 * `supportsNotes` (+ `notes`) to get the notes item/editor/preview. Without them the row is the
 * template shape — a click-to-rename title and no checkbox/notes column.
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
  onUpdate,
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
  onUpdate: (t: T, patch: TaskPatch) => void;
  onDelete: (t: T) => void;
  onMove: (t: T, dir: -1 | 1) => void;
}) {
  const [editing, setEditing] = useState<'title' | 'notes' | 'timeframe' | null>(null);
  const [draft, setDraft] = useState('');

  const timeframe = formatTimeframe(task.minDaysBefore, task.maxDaysBefore);

  function startEdit(field: 'title' | 'notes') {
    setDraft(field === 'title' ? task.title : notes ?? '');
    setEditing(field);
  }

  function saveEdit() {
    if (editing === 'title') {
      const title = draft.trim();
      if (title && title !== task.title) onUpdate(task, { title });
    } else if (editing === 'notes') {
      const next = draft.trim() || null;
      if (next !== notes) onUpdate(task, { notes: next });
    }
    setEditing(null);
  }

  return (
    <li className="border-b border-edge/50 last:border-b-0">
      <div className="flex items-center gap-3 px-4 py-2.5">
        {onToggle && (
          <button
            role="checkbox"
            aria-checked={isDone}
            aria-label={task.title}
            disabled={readOnly}
            onClick={() => !readOnly && onToggle(task)}
            className={`grid h-6 w-6 shrink-0 place-items-center rounded-md border transition ${
              isDone
                ? 'border-accent bg-accent text-strong'
                : 'border-edge bg-panel hover:border-accent'
            } ${readOnly ? 'cursor-default opacity-70 hover:border-edge' : ''}`}
          >
            {isDone && '✓'}
          </button>
        )}

        {editing === 'title' ? (
          <input
            autoFocus
            className={inputClass}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onBlur={saveEdit}
            onKeyDown={(e) => {
              if (e.key === 'Enter') saveEdit();
              if (e.key === 'Escape') setEditing(null);
            }}
          />
        ) : (
          <button
            className={`flex-1 text-left text-sm ${isDone ? 'text-subtle line-through' : 'text-strong'} ${readOnly ? 'cursor-default' : ''}`}
            disabled={readOnly}
            onClick={() => {
              if (readOnly) return;
              // Release rows toggle done on title click; template rows (no onToggle) rename instead.
              if (onToggle) onToggle(task);
              else startEdit('title');
            }}
          >
            {task.title}
            {timeframe && (
              <span className="ml-2 whitespace-nowrap text-xs text-accent/80">· {timeframe}</span>
            )}
            {supportsNotes && notes && (
              <span className="ml-1.5 text-xs text-subtle" title="Has notes" aria-label="Has notes">
                ✎
              </span>
            )}
          </button>
        )}

        {!readOnly && (
          <div className="flex shrink-0 items-center">
            <ReorderArrows isFirst={isFirst} isLast={isLast} onMove={(dir) => onMove(task, dir)} />
            <div className="ml-3">
              <RowMenu>
                {(close) => (
                  <>
                    <MenuItem onClick={() => { close(); startEdit('title'); }}>Rename</MenuItem>
                    {supportsNotes && (
                      <MenuItem onClick={() => { close(); startEdit('notes'); }}>
                        {notes ? 'Edit notes' : 'Add notes'}
                      </MenuItem>
                    )}
                    {task.phase === Phase.Pre && (
                      <MenuItem onClick={() => { close(); setEditing('timeframe'); }}>
                        {timeframe ? 'Edit timeframe' : 'Set timeframe'}
                      </MenuItem>
                    )}
                    <MovePhaseItems task={task} onUpdate={onUpdate} close={close} />
                    <MenuItem tone="danger" onClick={() => { close(); onDelete(task); }}>
                      Delete
                    </MenuItem>
                  </>
                )}
              </RowMenu>
            </div>
          </div>
        )}
      </div>

      {editing === 'notes' && (
        <div className="px-4 pb-3 pl-12">
          <textarea
            autoFocus
            rows={2}
            className={inputClass}
            placeholder="Notes"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onBlur={saveEdit}
            onKeyDown={(e) => {
              if (e.key === 'Escape') setEditing(null);
            }}
          />
        </div>
      )}
      {editing === 'timeframe' && (
        <TimeframeEditor
          min={task.minDaysBefore}
          max={task.maxDaysBefore}
          indent={!!onToggle}
          onSave={(min, max) => { onUpdate(task, { minDaysBefore: min, maxDaysBefore: max }); setEditing(null); }}
          onCancel={() => setEditing(null)}
        />
      )}
      {supportsNotes && editing !== 'notes' && notes && (
        <p className="px-4 pb-2.5 pl-12 text-xs text-muted">{notes}</p>
      )}
    </li>
  );
}
