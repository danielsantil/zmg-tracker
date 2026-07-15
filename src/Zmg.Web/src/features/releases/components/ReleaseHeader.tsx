import type { ReleaseDetail } from '@/types';
import { IdentifierWarning, ProgressBar, StatusBadge, TypeBadge } from '@/components';
import { formatCountdown } from '@/lib/format';

export function ReleaseHeader({
  release,
  done,
  total,
}: {
  release: ReleaseDetail;
  done: number;
  total: number;
}) {
  const countdown = formatCountdown(release.status, release.releaseDate);

  return (
    <div className="mb-6 flex gap-4 rounded-xl border border-edge bg-panel p-4">
      <div className="hidden h-24 w-24 shrink-0 overflow-hidden rounded-lg bg-edge sm:block">
        {release.coverUrl ? (
          <img src={release.coverUrl} alt="" className="h-full w-full object-cover" />
        ) : (
          <div className="grid h-full place-items-center text-3xl font-semibold text-slate-600">
            {release.title.slice(0, 1).toUpperCase()}
          </div>
        )}
      </div>
      <div className="min-w-0 flex-1">
        <div className="flex items-start justify-between gap-2">
          <h1 className="text-xl font-semibold text-white">
            {release.title} <span className="text-slate-400">— {release.mainArtistName}</span>
          </h1>
          <div className="flex items-center gap-1.5">
            {release.needsIdentifierWarning && <IdentifierWarning />}
            <StatusBadge status={release.status} />
          </div>
        </div>
        <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-slate-400">
          <TypeBadge type={release.type} />
          <span className="whitespace-nowrap">{release.releaseDate}</span>
          <span className="whitespace-nowrap">· {done}/{total} done</span>
          {countdown && <span className="whitespace-nowrap text-accent">· {countdown}</span>}
        </div>
        <div className="mt-3">
          <ProgressBar done={done} total={total} />
        </div>
      </div>
    </div>
  );
}
