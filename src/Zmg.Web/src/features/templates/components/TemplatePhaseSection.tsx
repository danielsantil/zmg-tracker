import type { Phase } from '@/types';
import { InlineAddForm } from '@/components';
import { phaseLabels } from '@/lib/phase';
import type { TemplateTaskDto } from '@/types';
import { TemplateTaskRow, type TemplatePatch } from './TemplateTaskRow';

export function TemplatePhaseSection({
  phase,
  tasks,
  onAdd,
  onUpdate,
  onDelete,
  onMove,
}: {
  phase: Phase;
  tasks: TemplateTaskDto[];
  onAdd: (title: string) => void;
  onUpdate: (t: TemplateTaskDto, patch: TemplatePatch) => void;
  onDelete: (t: TemplateTaskDto) => void;
  onMove: (t: TemplateTaskDto, dir: -1 | 1) => void;
}) {
  return (
    <section className="overflow-hidden rounded-xl border border-edge bg-panel">
      <div className="flex items-center justify-between px-4 py-3">
        <span className="flex items-center gap-2 font-semibold text-white">
          {phaseLabels[phase].toUpperCase()}
          <span className="text-sm font-normal text-slate-400">({tasks.length})</span>
        </span>
      </div>

      <div className="border-t border-edge">
        {tasks.length === 0 ? (
          <p className="px-4 py-3 text-sm text-slate-500">No tasks in this phase.</p>
        ) : (
          <ul>
            {tasks.map((t, i) => (
              <TemplateTaskRow
                key={t.id}
                task={t}
                isFirst={i === 0}
                isLast={i === tasks.length - 1}
                onUpdate={onUpdate}
                onDelete={onDelete}
                onMove={onMove}
              />
            ))}
          </ul>
        )}

        <InlineAddForm addLabel="+ Add task" placeholder="New task title" onAdd={onAdd} />
      </div>
    </section>
  );
}
