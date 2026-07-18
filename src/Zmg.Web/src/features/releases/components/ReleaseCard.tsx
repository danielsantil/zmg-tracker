import type { ReleaseListItem } from '@/types';
import { MenuItem, ProgressBar, RowMenu, SoftWarning, StatusBadge, TypeBadge } from '@/components';
import { formatCountdown, formatReleaseDate } from '@/lib/format';

/**
 * The compact release card. Actions live in the kebab so it stays short enough to stack in a modal.
 * Home renders it with a cover (whose click opens the release); the calendar preview has neither, so
 * it opts into the explicit "Open release →" link instead.
 */
export function ReleaseCard({
  r,
  onOpen,
  onEdit,
  onArchive,
  showCover = false,
  showOpenLink = false,
}: {
  r: ReleaseListItem;
  onOpen: () => void;
  onEdit: () => void;
  onArchive: () => void;
  showCover?: boolean;
  showOpenLink?: boolean;
}) {
  const countdown = formatCountdown(r.releaseDate);

  return (
    <div className="flex flex-col overflow-hidden rounded-xl border border-edge bg-panel">
      {showCover && (
        <button onClick={onOpen} className="block aspect-[16/9] w-full bg-edge text-left">
          {r.coverUrl ? (
            <img src={r.coverUrl} alt="" className="h-full w-full object-cover" />
          ) : (
            <div className="grid h-full place-items-center text-3xl font-semibold text-subtle">
              {r.title.slice(0, 1).toUpperCase()}
            </div>
          )}
        </button>
      )}
      <div className="flex flex-1 flex-col gap-2 p-3">
        <div>
          <div className="flex items-start justify-between gap-2">
            <button onClick={onOpen} className="text-left font-semibold text-strong hover:text-accent">
              {r.title}
            </button>
            <div className="flex shrink-0 items-center gap-1.5">
              <SoftWarning warnings={r.warnings} />
              <StatusBadge status={r.status} />
              <RowMenu label="Release actions">
                {(close) => (
                  <>
                    <MenuItem
                      onClick={() => {
                        close();
                        onEdit();
                      }}
                    >
                      Edit
                    </MenuItem>
                    {/* Archive affordance follows the server's canArchive (upcoming & not archived). */}
                    {r.canArchive && (
                      <MenuItem
                        tone="archive"
                        onClick={() => {
                          close();
                          onArchive();
                        }}
                      >
                        Archive
                      </MenuItem>
                    )}
                  </>
                )}
              </RowMenu>
            </div>
          </div>
          <p className="text-sm text-muted">{r.mainArtistName}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2 text-xs text-muted">
          <TypeBadge type={r.type} />
          <span>{formatReleaseDate(r.releaseDate)}</span>
          {countdown && <span className="text-accent">· {countdown}</span>}
        </div>
        <div className="mt-auto">
          <ProgressBar
            done={r.doneTasks}
            total={r.totalTasks}
            slim
            label={`${r.doneTasks} / ${r.totalTasks} tasks`}
          />
        </div>
        {showOpenLink && (
          <button onClick={onOpen} className="w-fit text-left text-sm text-accent hover:underline">
            Open release →
          </button>
        )}
      </div>
    </div>
  );
}
