/**
 * `slim` + `label` are the compact-card dressing (M21): a thinner bar under a
 * "X / Y tasks" caption. Left alone, it keeps the fuller "N/M done · P%" default.
 */
export function ProgressBar({
  done,
  total,
  slim = false,
  label,
}: {
  done: number;
  total: number;
  slim?: boolean;
  label?: string;
}) {
  const pct = total === 0 ? 0 : Math.round((done / total) * 100);
  return (
    <div>
      <div className={`w-full overflow-hidden rounded-full bg-edge ${slim ? 'h-1.5' : 'h-2'}`}>
        <div
          className="h-full rounded-full bg-accent transition-all"
          style={{ width: `${pct}%` }}
        />
      </div>
      <div className="mt-1 text-xs text-muted">
        {label ?? `${done}/${total} done · ${pct}%`}
      </div>
    </div>
  );
}
