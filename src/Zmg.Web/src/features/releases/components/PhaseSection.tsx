import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { Phase } from '@/types';
import { Button } from '@/components';
import { phaseLabelKeys } from '@/lib/phase';
import { TaskRow, type ChecklistTask } from './TaskRow';

/**
 * A phase's task list plus its add button, generic over release and template tasks (M24.5).
 * The release-only behaviour is opt-in via `getIsDone` (+ `onToggle`): it turns on the done count,
 * the ✓ / collapse-when-complete header, and per-row checkboxes. Templates omit it and render a
 * plain, always-open section with a bare task count.
 *
 * Adding goes through the same modal as editing (v2.9), so the old inline add form is gone from here:
 * a task is born with both languages rather than acquiring the second one later.
 */
export function PhaseSection<T extends ChecklistTask>({
  phase,
  tasks,
  readOnly = false,
  onToggle,
  getIsDone,
  getNotes,
  onAdd,
  onEdit,
  onDelete,
  onMove,
}: {
  phase: Phase;
  tasks: T[];
  readOnly?: boolean;
  onToggle?: (t: T) => void;
  getIsDone?: (t: T) => boolean;
  getNotes?: (t: T) => string | null;
  onAdd: (phase: Phase) => void;
  onEdit: (t: T) => void;
  onDelete: (t: T) => void;
  onMove: (t: T, dir: -1 | 1) => void;
}) {
  const { t } = useTranslation();
  const total = tasks.length;
  const done = getIsDone ? tasks.filter(getIsDone).length : 0;
  const tracksProgress = !!getIsDone;
  const allDone = tracksProgress && total > 0 && done === total;
  // Release phases collapse once fully done; template phases don't collapse at all.
  const collapsible = tracksProgress;
  const [open, setOpen] = useState(!(collapsible && allDone));

  const header = (
    <span className="flex items-center gap-2 font-semibold text-strong">
      {collapsible && <span className="text-subtle">{open ? '▾' : '▸'}</span>}
      {t(phaseLabelKeys[phase]).toUpperCase()}
      <span className="text-sm font-normal text-muted">
        {tracksProgress ? `(${done}/${total})` : `(${total})`}
      </span>
      {allDone && <span className="text-okFg">✓</span>}
    </span>
  );

  return (
    <section className="overflow-hidden rounded-xl border border-edge bg-panel">
      {collapsible ? (
        <button
          className="flex w-full items-center justify-between px-4 py-3 text-left"
          onClick={() => setOpen((o) => !o)}
        >
          {header}
        </button>
      ) : (
        <div className="flex items-center justify-between px-4 py-3">{header}</div>
      )}

      {open && (
        <div className="border-t border-edge">
          {tasks.length === 0 ? (
            <p className="px-4 py-3 text-sm text-subtle">{t('tasks.emptyPhase')}</p>
          ) : (
            <ul>
              {tasks.map((task, i) => (
                <TaskRow
                  key={task.id}
                  task={task}
                  isFirst={i === 0}
                  isLast={i === tasks.length - 1}
                  readOnly={readOnly}
                  onToggle={onToggle}
                  isDone={getIsDone?.(task) ?? false}
                  supportsNotes={!!getNotes}
                  notes={getNotes?.(task) ?? null}
                  onEdit={onEdit}
                  onDelete={onDelete}
                  onMove={onMove}
                />
              ))}
            </ul>
          )}

          {!readOnly && (
            <div className="border-t border-edge/50 px-4 py-2.5">
              <Button variant="ghost" onClick={() => onAdd(phase)}>
                {t('tasks.addTask')}
              </Button>
            </div>
          )}
        </div>
      )}
    </section>
  );
}
