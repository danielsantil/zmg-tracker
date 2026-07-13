import { Link } from 'react-router-dom';
import type { PendingAction } from '@/types';
import { PendingKind } from '@/types';

/** Aggregate pending actions across all releases (M10), task-due nearest-first then data items. */
export function PendingSection({ pending }: { pending: PendingAction[] }) {
  const nextPage = (pending: PendingAction) => {
    if (pending.kind === PendingKind.TaskDue) {
      return `/releases/${pending.releaseId}`;
    } else {
      return `/releases/${pending.releaseId}/edit`;
    }
  };

  if (pending.length === 0) return null;
  return (
    <section className="mb-6 overflow-hidden rounded-xl border border-amber-500/25 bg-amber-500/[0.06]">
      <div className="border-b border-amber-500/20 px-4 py-3 font-semibold text-amber-200">
        Pending Tasks <span className="text-sm font-normal text-amber-200/70">({pending.length})</span>
      </div>
      <ul>
        {pending.map((p, i) => (
          <li key={`${p.releaseId}-${p.taskId ?? p.kind}-${i}`} className="border-b border-amber-500/10 last:border-b-0">
            <Link
              to={nextPage(p)}
              className="flex items-center justify-between gap-3 px-4 py-2.5 hover:bg-amber-500/[0.06]"
            >
              <span className="min-w-0">
                <span className="text-sm text-slate-100">{p.label}</span>
                <span className="ml-2 text-xs text-slate-400">
                  {p.releaseTitle} · {p.artistName}
                </span>
              </span>
              <span className="shrink-0 text-xs">
                {p.kind === PendingKind.TaskDue && p.daysToRelease != null ? (
                  <span className="whitespace-nowrap text-accent">
                    {p.daysToRelease === 0 ? 'Releases today' : `${p.daysToRelease} days to release`}
                  </span>
                ) : (
                  <span className="whitespace-nowrap text-amber-300">Data</span>
                )}
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
