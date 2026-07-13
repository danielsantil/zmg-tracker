import { useState } from 'react';
import type { ReleaseTaskDto } from '@/types';
import { Phase } from '@/types';
import { MenuItem, RowMenu, inputClass } from '@/components';
import { formatTimeframe } from '@/lib/format';
import { TimeframeEditor } from './TimeframeEditor';
import { MovePhaseItems } from './MovePhaseItems';

export type TaskPatch = Partial<Pick<ReleaseTaskDto, 'title' | 'phase' | 'notes' | 'minDaysBefore' | 'maxDaysBefore'>>;

export function TaskRow({
  task,
  isFirst,
  isLast,
  onToggle,
  onUpdate,
  onDelete,
  onMove,
}: {
  task: ReleaseTaskDto;
  isFirst: boolean;
  isLast: boolean;
  onToggle: (t: ReleaseTaskDto) => void;
  onUpdate: (t: ReleaseTaskDto, patch: TaskPatch) => void;
  onDelete: (t: ReleaseTaskDto) => void;
  onMove: (t: ReleaseTaskDto, dir: -1 | 1) => void;
}) {
  const [editing, setEditing] = useState<'title' | 'notes' | 'timeframe' | null>(null);
  const [draft, setDraft] = useState('');

  const timeframe = formatTimeframe(task.minDaysBefore, task.maxDaysBefore);

  function startEdit(field: 'title' | 'notes') {
    setDraft(field === 'title' ? task.title : task.notes ?? '');
    setEditing(field);
  }

  function saveEdit() {
    if (editing === 'title') {
      const title = draft.trim();
      if (title && title !== task.title) onUpdate(task, { title });
    } else if (editing === 'notes') {
      const notes = draft.trim() || null;
      if (notes !== task.notes) onUpdate(task, { notes });
    }
    setEditing(null);
  }

  return (
    <li className="border-b border-edge/50 last:border-b-0">
      <div className="flex items-center gap-3 px-4 py-2.5">
        <button
          role="checkbox"
          aria-checked={task.isDone}
          aria-label={task.title}
          onClick={() => onToggle(task)}
          className={`grid h-6 w-6 shrink-0 place-items-center rounded-md border transition ${
            task.isDone
              ? 'border-accent bg-accent text-white'
              : 'border-edge bg-panel hover:border-accent'
          }`}
        >
          {task.isDone && '✓'}
        </button>

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
            className={`flex-1 text-left text-sm ${task.isDone ? 'text-slate-500 line-through' : 'text-slate-100'}`}
            onClick={() => onToggle(task)}
          >
            {task.title}
            {timeframe && (
              <span className="ml-2 whitespace-nowrap text-xs text-accent/80">· {timeframe}</span>
            )}
            {task.notes && (
              <span className="ml-1.5 text-xs text-slate-500" title="Has notes" aria-label="Has notes">
                ✎
              </span>
            )}
          </button>
        )}

        <RowMenu>
          {(close) => (
            <>
              <MenuItem onClick={() => { close(); startEdit('title'); }}>Rename</MenuItem>
              <MenuItem onClick={() => { close(); startEdit('notes'); }}>
                {task.notes ? 'Edit notes' : 'Add notes'}
              </MenuItem>
              {task.phase === Phase.Pre && (
                <MenuItem onClick={() => { close(); setEditing('timeframe'); }}>
                  {timeframe ? 'Edit timeframe' : 'Set timeframe'}
                </MenuItem>
              )}
              <MovePhaseItems task={task} onUpdate={onUpdate} close={close} />
              {!isFirst && (
                <MenuItem onClick={() => { close(); onMove(task, -1); }}>Move up</MenuItem>
              )}
              {!isLast && (
                <MenuItem onClick={() => { close(); onMove(task, 1); }}>Move down</MenuItem>
              )}
              <MenuItem danger onClick={() => { close(); onDelete(task); }}>
                Delete
              </MenuItem>
            </>
          )}
        </RowMenu>
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
          task={task}
          onSave={(min, max) => { onUpdate(task, { minDaysBefore: min, maxDaysBefore: max }); setEditing(null); }}
          onCancel={() => setEditing(null)}
        />
      )}
      {editing !== 'notes' && task.notes && (
        <p className="px-4 pb-2.5 pl-12 text-xs text-slate-400">{task.notes}</p>
      )}
    </li>
  );
}
