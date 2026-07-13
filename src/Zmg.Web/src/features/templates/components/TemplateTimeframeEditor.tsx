import { useState } from 'react';
import type { TemplateTaskDto } from '@/types';
import { Button, inputClass } from '@/components';

/** Inline "days before release" editor for a Pre template task (v1.1 M8). */
export function TemplateTimeframeEditor({
  task,
  onSave,
  onCancel,
}: {
  task: TemplateTaskDto;
  onSave: (min: number | null, max: number | null) => void;
  onCancel: () => void;
}) {
  const [min, setMin] = useState(task.minDaysBefore?.toString() ?? '');
  const [max, setMax] = useState(task.maxDaysBefore?.toString() ?? '');

  function parse(v: string): number | null {
    const n = parseInt(v, 10);
    return Number.isFinite(n) && n >= 0 ? n : null;
  }

  return (
    <div className="flex flex-wrap items-center gap-2 px-4 pb-3 text-sm text-slate-300">
      <span className="text-xs text-slate-500">Days before release:</span>
      <input
        autoFocus
        type="number"
        min={0}
        className={`${inputClass} w-20`}
        placeholder="min"
        value={min}
        onChange={(e) => setMin(e.target.value)}
      />
      <span className="text-slate-500">–</span>
      <input
        type="number"
        min={0}
        className={`${inputClass} w-20`}
        placeholder="max"
        value={max}
        onChange={(e) => setMax(e.target.value)}
      />
      <Button onClick={() => onSave(parse(min), parse(max))}>Save</Button>
      <Button variant="ghost" onClick={() => onSave(null, null)}>Clear</Button>
      <Button variant="ghost" onClick={onCancel}>Cancel</Button>
    </div>
  );
}
