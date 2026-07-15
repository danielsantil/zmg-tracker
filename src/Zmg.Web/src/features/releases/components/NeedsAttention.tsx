import { Link } from 'react-router-dom';
import type { PendingAction } from '@/types';
import { PendingKind } from '@/types';

/**
 * "Needs attention" block (M10; reworked M14) — this release's pending actions, computed server-side
 * from the loaded detail payload. Task-due items show days-to-release; the rolled-up "missing ISRC"
 * rows for the release's songs link into the catalog. Other data items (missing UPC, empty album) don't.
 */
export function NeedsAttention({ actions }: { actions: PendingAction[] }) {
  return (
    <div className="mb-6 overflow-hidden rounded-xl border border-amber-500/25 bg-amber-500/[0.06]">
      <div className="border-b border-amber-500/20 px-4 py-2.5 text-sm font-semibold text-amber-200">
        Needs attention
      </div>
      <ul className="px-4 py-2">
        {actions.map((a, i) => (
          <li key={`${a.taskId ?? a.songId ?? a.kind}-${i}`} className="flex items-baseline gap-2 py-1 text-sm text-slate-200">
            <span className="text-amber-300">•</span>
            {a.kind === PendingKind.MissingIsrc && a.songId ? (
              <Link to={`/catalog/${a.songId}`} className="text-slate-200 underline decoration-dotted hover:text-white">
                {a.label} — {a.subject}
              </Link>
            ) : (
              <span>{a.label}</span>
            )}
            {a.kind === PendingKind.TaskDue && a.daysToRelease != null && (
              <span className="text-xs text-accent">
                — {a.daysToRelease === 0 ? 'releases today' : `${a.daysToRelease} days to release`}
              </span>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}
