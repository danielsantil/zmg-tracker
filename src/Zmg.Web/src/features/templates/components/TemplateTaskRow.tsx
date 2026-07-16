import { useState } from 'react';
import type { TemplateTaskDto } from '@/types';
import { Phase } from '@/types';
import { MenuItem, RowMenu, inputClass } from '@/components';
import { formatTimeframe } from '@/lib/format';
import { TemplateTimeframeEditor } from './TemplateTimeframeEditor';
import { TemplateMovePhaseItems } from './TemplateMovePhaseItems';

export type TemplatePatch = Partial<Pick<TemplateTaskDto, 'title' | 'phase' | 'minDaysBefore' | 'maxDaysBefore'>>;

export function TemplateTaskRow({
  task,
  isFirst,
  isLast,
  onUpdate,
  onDelete,
  onMove,
}: {
  task: TemplateTaskDto;
  isFirst: boolean;
  isLast: boolean;
  onUpdate: (t: TemplateTaskDto, patch: TemplatePatch) => void;
  onDelete: (t: TemplateTaskDto) => void;
  onMove: (t: TemplateTaskDto, dir: -1 | 1) => void;
}) {
  const [editing, setEditing] = useState<'title' | 'timeframe' | null>(null);
  const [draft, setDraft] = useState('');

  const timeframe = formatTimeframe(task.minDaysBefore, task.maxDaysBefore);

  function startEdit() {
    setDraft(task.title);
    setEditing('title');
  }

  function saveEdit() {
    const title = draft.trim();
    if (title && title !== task.title) onUpdate(task, { title });
    setEditing(null);
  }

  return (
    <li className="border-b border-edge/50 last:border-b-0">
      <div className="flex items-center gap-3 px-4 py-2.5">
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
          <button className="flex-1 text-left text-sm text-slate-100" onClick={startEdit}>
            {task.title}
            {timeframe && (
              <span className="ml-2 whitespace-nowrap text-xs text-accent/80">· {timeframe}</span>
            )}
          </button>
        )}

        <RowMenu>
          {(close) => (
            <>
              <MenuItem onClick={() => { close(); startEdit(); }}>Rename</MenuItem>
              {task.phase === Phase.Pre && (
                <MenuItem onClick={() => { close(); setEditing('timeframe'); }}>
                  {timeframe ? 'Edit timeframe' : 'Set timeframe'}
                </MenuItem>
              )}
              <TemplateMovePhaseItems task={task} onUpdate={onUpdate} close={close} />
              {!isFirst && (
                <MenuItem onClick={() => { close(); onMove(task, -1); }}>Move up</MenuItem>
              )}
              {!isLast && (
                <MenuItem onClick={() => { close(); onMove(task, 1); }}>Move down</MenuItem>
              )}
              <MenuItem tone="danger" onClick={() => { close(); onDelete(task); }}>
                Delete
              </MenuItem>
            </>
          )}
        </RowMenu>
      </div>

      {editing === 'timeframe' && (
        <TemplateTimeframeEditor
          task={task}
          onSave={(min, max) => { onUpdate(task, { minDaysBefore: min, maxDaysBefore: max }); setEditing(null); }}
          onCancel={() => setEditing(null)}
        />
      )}
    </li>
  );
}
