import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { PendingAction } from '@/types';
import { PendingKind } from '@/types';

/** Where a pending action routes: song-owned items go to the catalog, release-owned to the release. */
function nextPage(p: PendingAction) {
  switch (p.kind) {
    case PendingKind.MissingIsrc:
      return `/catalog/${p.songId}`;
    case PendingKind.MissingUpc:
      return `/releases/${p.releaseId}/edit`;
    default: // TaskDue, EmptyAlbum
      return `/releases/${p.releaseId}`;
  }
}

/**
 * Aggregate pending actions across all releases and songs (M10; reworked M14): task-due nearest-first,
 * then the data kinds by subject. Collapsible header (PhaseSection-style); the list scrolls past ~4 rows.
 */
export function PendingSection({ pending }: { pending: PendingAction[] }) {
  const [open, setOpen] = useState(true);

  if (pending.length === 0) return null;
  return (
    <section className="mb-6 overflow-hidden rounded-xl border border-amber-500/25 bg-amber-500/[0.06]">
      <button
        className="flex w-full items-center gap-2 px-4 py-3 text-left font-semibold text-amber-200"
        onClick={() => setOpen((o) => !o)}
      >
        <span className="text-amber-200/60">{open ? '▾' : '▸'}</span>
        Pending Tasks <span className="text-sm font-normal text-amber-200/70">({pending.length})</span>
      </button>
      {open && (
        <ul className="max-h-[11rem] overflow-y-auto border-t border-amber-500/20">
          {pending.map((p, i) => (
            <li key={`${p.releaseId ?? p.songId}-${p.taskId ?? p.kind}-${i}`} className="border-b border-amber-500/10 last:border-b-0">
              <Link
                to={nextPage(p)}
                className="flex items-center justify-between gap-3 px-4 py-2.5 hover:bg-amber-500/[0.06]"
              >
                <span className="min-w-0">
                  <span className="text-sm text-strong">{p.label}</span>
                  <span className="ml-2 text-xs text-muted">
                    {p.subject} · {p.artistName}
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
      )}
    </section>
  );
}
