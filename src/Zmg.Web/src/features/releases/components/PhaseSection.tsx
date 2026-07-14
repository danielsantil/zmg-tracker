import { useState } from 'react';
import type { Phase } from '@/types';
import type { ReleaseTaskDto } from '@/types';
import { InlineAddForm } from '@/components';
import { phaseLabels } from '@/lib/phase';
import { TaskRow, type TaskPatch } from './TaskRow';

export function PhaseSection({
  phase,
  tasks,
  readOnly = false,
  onToggle,
  onAdd,
  onUpdate,
  onDelete,
  onMove,
}: {
  phase: Phase;
  tasks: ReleaseTaskDto[];
  readOnly?: boolean;
  onToggle: (t: ReleaseTaskDto) => void;
  onAdd: (title: string) => void;
  onUpdate: (t: ReleaseTaskDto, patch: TaskPatch) => void;
  onDelete: (t: ReleaseTaskDto) => void;
  onMove: (t: ReleaseTaskDto, dir: -1 | 1) => void;
}) {
  const done = tasks.filter((t) => t.isDone).length;
  const total = tasks.length;
  const allDone = total > 0 && done === total;
  // Fully-done phases collapse by default; tap the header to expand.
  const [open, setOpen] = useState(!allDone);

  return (
    <section className="overflow-hidden rounded-xl border border-edge bg-panel">
      <button
        className="flex w-full items-center justify-between px-4 py-3 text-left"
        onClick={() => setOpen((o) => !o)}
      >
        <span className="flex items-center gap-2 font-semibold text-white">
          <span className="text-slate-500">{open ? '▾' : '▸'}</span>
          {phaseLabels[phase].toUpperCase()}
          <span className="text-sm font-normal text-slate-400">
            ({done}/{total})
          </span>
          {allDone && <span className="text-emerald-400">✓</span>}
        </span>
      </button>

      {open && (
        <div className="border-t border-edge">
          {tasks.length === 0 ? (
            <p className="px-4 py-3 text-sm text-slate-500">No tasks in this phase.</p>
          ) : (
            <ul>
              {tasks.map((t, i) => (
                <TaskRow
                  key={t.id}
                  task={t}
                  isFirst={i === 0}
                  isLast={i === tasks.length - 1}
                  readOnly={readOnly}
                  onToggle={onToggle}
                  onUpdate={onUpdate}
                  onDelete={onDelete}
                  onMove={onMove}
                />
              ))}
            </ul>
          )}

          {!readOnly && <InlineAddForm addLabel="+ Add task" placeholder="New task title" onAdd={onAdd} />}
        </div>
      )}
    </section>
  );
}
