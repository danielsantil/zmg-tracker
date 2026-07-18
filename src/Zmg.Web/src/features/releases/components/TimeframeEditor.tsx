import { useState } from 'react';
import { Button, inputClass } from '@/components';

/**
 * Inline "days before release" editor for a Pre task (v1.1 M8). Min is display-only in the hint;
 * the max drives pending calculations. Leaving both blank clears the timeframe.
 *
 * Shared by release tasks and template tasks — both carry `min/maxDaysBefore`. Release rows sit a
 * checkbox-width in from the edge, so they pass `indent`; template rows have no checkbox column.
 */
export function TimeframeEditor({
  min: initialMin,
  max: initialMax,
  indent = false,
  onSave,
  onCancel,
}: {
  min: number | null;
  max: number | null;
  indent?: boolean;
  onSave: (min: number | null, max: number | null) => void;
  onCancel: () => void;
}) {
  const [min, setMin] = useState(initialMin?.toString() ?? '');
  const [max, setMax] = useState(initialMax?.toString() ?? '');

  function parse(v: string): number | null {
    const n = parseInt(v, 10);
    return Number.isFinite(n) && n >= 0 ? n : null;
  }

  return (
    <div className={`flex flex-wrap items-center gap-2 px-4 pb-3 text-sm text-body ${indent ? 'pl-12' : ''}`}>
      <span className="text-xs text-subtle">Days before release:</span>
      <input
        autoFocus
        type="number"
        min={0}
        className={`${inputClass} w-20`}
        placeholder="min"
        value={min}
        onChange={(e) => setMin(e.target.value)}
      />
      <span className="text-subtle">–</span>
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
