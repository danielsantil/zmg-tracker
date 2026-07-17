import type { ReactNode } from 'react';

/** Shared row class for a clickable table row (was copy-pasted across all 5 list tables). */
export const dataRowClass =
  'cursor-pointer border-b border-edge/50 last:border-b-0 hover:bg-edge/40';

/**
 * The bordered, horizontally-scrollable table shell shared by every list page. `overflow-x-auto` is
 * the M22 mobile-clipping fix — wide tables scroll inside their own box so the page body never
 * scrolls sideways. Callers pass column headers and render their own `<tr>`s (using `dataRowClass`).
 */
export function DataTable({ headers, children }: { headers: string[]; children: ReactNode }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-edge bg-panel">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-edge text-xs uppercase tracking-wide text-slate-500">
          <tr>
            {headers.map((h) => (
              <th key={h} className="px-4 py-3 font-medium">
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>{children}</tbody>
      </table>
    </div>
  );
}
