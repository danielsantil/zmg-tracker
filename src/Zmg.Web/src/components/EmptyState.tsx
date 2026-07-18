import type { ReactNode } from 'react';

/**
 * The dashed-border empty placeholder (was 8 hand-written copies of the same container). Callers
 * supply the copy — a plain string for the "no rows match" cases, or richer content with links
 * (Home's "no upcoming releases", the forms' "need at least one artist" blocks).
 */
export function EmptyState({ children }: { children: ReactNode }) {
  return (
    <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center text-muted">
      {children}
    </div>
  );
}
