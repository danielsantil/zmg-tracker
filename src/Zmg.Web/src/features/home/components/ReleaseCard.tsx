import type { ReleaseListItem } from '@/types';
import { Button, IdentifierWarning, ProgressBar, StatusBadge, TypeBadge } from '@/components';
import { formatCountdown } from '@/lib/format';

export function ReleaseCard({
  r,
  onOpen,
  onEdit,
  onDelete,
}: {
  r: ReleaseListItem;
  onOpen: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const countdown = formatCountdown(r.status, r.releaseDate);

  return (
    <div className="flex flex-col overflow-hidden rounded-xl border border-edge bg-panel">
      <button onClick={onOpen} className="block aspect-[16/9] w-full bg-edge text-left">
        {r.coverUrl ? (
          <img src={r.coverUrl} alt="" className="h-full w-full object-cover" />
        ) : (
          <div className="grid h-full place-items-center text-3xl font-semibold text-slate-600">
            {r.title.slice(0, 1).toUpperCase()}
          </div>
        )}
      </button>
      <div className="flex flex-1 flex-col gap-3 p-4">
        <div>
          <div className="flex items-start justify-between gap-2">
            <button onClick={onOpen} className="text-left font-semibold text-white hover:text-accent">
              {r.title}
            </button>
            <div className="flex items-center gap-1.5">
              {r.needsIdentifierWarning && <IdentifierWarning upc={r.upc} isrc={r.isrc} />}
              <StatusBadge status={r.status} />
            </div>
          </div>
          <p className="text-sm text-slate-400">{r.mainArtistName}</p>
        </div>
        <div className="flex items-center gap-2 text-xs text-slate-400">
          <TypeBadge type={r.type} />
          <span>{r.releaseDate}</span>
          {countdown && <span className="text-accent">· {countdown}</span>}
        </div>
        <div className="mt-auto">
          <ProgressBar done={r.doneTasks} total={r.totalTasks} />
        </div>
        <div className="flex gap-2">
          <Button variant="ghost" onClick={onEdit}>
            Edit
          </Button>
          <Button variant="danger" onClick={onDelete}>
            Delete
          </Button>
        </div>
      </div>
    </div>
  );
}
