import type { ReactNode } from 'react';
import clsx from 'clsx';

/** Shared row class for a clickable table row (was copy-pasted across all 5 list tables). */
export const dataRowClass =
  'cursor-pointer border-b border-edge/50 last:border-b-0 hover:bg-edge/40';

/**
 * A column header. `label` may be empty (e.g. the action column, which needs no title), and
 * `className` carries per-column tweaks like `text-right` or the responsive `hidden sm:table-cell`.
 */
export type TableHeader = { label: string; className?: string };

/**
 * The bordered, horizontally-scrollable table shell shared by every list page. `overflow-x-auto` is
 * the M22 mobile-clipping fix — wide tables scroll inside their own box so the page body never
 * scrolls sideways. Callers pass column headers and render their own `<tr>`s (using `dataRowClass`).
 */
export function DataTable({ headers, children }: { headers: TableHeader[]; children: ReactNode }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-edge bg-panel">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-edge text-xs uppercase tracking-wide text-subtle">
          <tr>
            {headers.map((h, i) => (
              <th key={h.label || i} className={clsx('px-4 py-3 font-medium', h.className)}>
                {h.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>{children}</tbody>
      </table>
    </div>
  );
}
