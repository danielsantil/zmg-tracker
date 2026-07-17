import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { ReleaseListItem } from '@/types';
import { ReleaseType } from '@/types';
import { Modal } from '@/components';
import { formatReleaseDate, todayIso } from '@/lib/format';
import {
  WEEKDAYS,
  addMonths,
  isInMonth,
  monthGrid,
  monthLabel,
  monthOf,
  type YearMonth,
} from '@/lib/calendar';
import { ReleaseCard } from './ReleaseCard';

/** Type-tinted so a day's releases are scannable at a glance without reading the titles. */
const chipTone: Record<ReleaseType, string> = {
  [ReleaseType.Single]: 'bg-accent/20 text-accent ring-1 ring-accent/30',
  [ReleaseType.Album]: 'bg-emerald-500/20 text-emerald-300 ring-1 ring-emerald-500/30',
};
const dotTone: Record<ReleaseType, string> = {
  [ReleaseType.Single]: 'bg-accent',
  [ReleaseType.Album]: 'bg-emerald-400',
};

/**
 * Month grid of releases (M22). One responsive grid at every size: `sm` and up shows title chips,
 * mobile shrinks to colored dots. Consumes the already-filtered `scope=all` list — no fetch of its
 * own — and groups by the raw `releaseDate` string to stay clear of timezone drift.
 */
export function ReleaseCalendar({
  releases,
  onArchive,
}: {
  releases: ReleaseListItem[];
  onArchive: (r: ReleaseListItem) => void;
}) {
  const navigate = useNavigate();
  const today = todayIso();
  const [ym, setYm] = useState<YearMonth>(() => monthOf(today));
  const [selected, setSelected] = useState<string | null>(null);

  const byDate = useMemo(() => {
    const map = new Map<string, ReleaseListItem[]>();
    for (const r of releases) {
      const day = map.get(r.releaseDate);
      if (day) day.push(r);
      else map.set(r.releaseDate, [r]);
    }
    return map;
  }, [releases]);

  // Nearest release still to come, in the current filter set — the header's jump chip.
  const next = useMemo(
    () =>
      releases
        .filter((r) => r.releaseDate >= today)
        .reduce<ReleaseListItem | null>(
          (best, r) => (!best || r.releaseDate < best.releaseDate ? r : best),
          null,
        ),
    [releases, today],
  );

  const weeks = useMemo(() => monthGrid(ym), [ym]);
  const selectedReleases = selected ? (byDate.get(selected) ?? []) : [];

  return (
    <div className="overflow-hidden rounded-xl border border-edge bg-panel">
      <div className="flex flex-wrap items-center gap-2 border-b border-edge px-3 py-3">
        <div className="flex items-center gap-1">
          <button
            onClick={() => setYm(addMonths(ym, -1))}
            aria-label="Previous month"
            className="rounded-lg px-2 py-1 text-slate-400 hover:bg-edge hover:text-white"
          >
            ‹
          </button>
          <button
            onClick={() => setYm(addMonths(ym, 1))}
            aria-label="Next month"
            className="rounded-lg px-2 py-1 text-slate-400 hover:bg-edge hover:text-white"
          >
            ›
          </button>
        </div>
        <h2 className="text-base font-semibold text-white">{monthLabel(ym)}</h2>
        <button
          onClick={() => setYm(monthOf(today))}
          className="rounded-lg bg-edge px-2.5 py-1 text-xs font-medium text-slate-200 hover:bg-edge/70"
        >
          Today
        </button>
        {next && (
          <button
            onClick={() => setYm(monthOf(next.releaseDate))}
            className="ml-auto truncate rounded-full bg-accent/15 px-2.5 py-1 text-xs font-medium text-accent ring-1 ring-accent/30 hover:bg-accent/25"
          >
            Next release · {formatReleaseDate(next.releaseDate)}
          </button>
        )}
      </div>

      <div className="grid grid-cols-7 border-b border-edge text-center text-[11px] uppercase tracking-wide text-slate-500">
        {WEEKDAYS.map((d) => (
          <div key={d} className="py-2">
            {/* Mobile cells are too narrow for "Wed" — the first letter still orients the grid. */}
            <span className="sm:hidden">{d.slice(0, 1)}</span>
            <span className="hidden sm:inline">{d}</span>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-7">
        {weeks.flat().map((iso) => {
          const day = byDate.get(iso) ?? [];
          const inMonth = isInMonth(iso, ym);
          const isToday = iso === today;
          return (
            <div
              key={iso}
              className={`min-h-[4.5rem] border-b border-r border-edge/50 p-1 last:border-r-0 sm:min-h-[6.5rem] sm:p-1.5 ${
                inMonth ? '' : 'bg-ink/40'
              }`}
            >
              <div
                className={`mb-1 text-center text-xs sm:text-left ${
                  isToday
                    ? 'mx-auto grid h-5 w-5 place-items-center rounded-full bg-accent font-semibold text-white sm:mx-0'
                    : inMonth
                      ? 'text-slate-400'
                      : 'text-slate-600'
                }`}
              >
                {Number(iso.slice(8, 10))}
              </div>

              {/* Desktop: up to two title chips, then a "+N more" spill into the same modal. */}
              <div className="hidden flex-col gap-1 sm:flex">
                {day.slice(0, 2).map((r) => (
                  <button
                    key={r.id}
                    onClick={() => setSelected(iso)}
                    title={r.title}
                    className={`truncate rounded px-1.5 py-0.5 text-left text-[11px] font-medium ${chipTone[r.type]}`}
                  >
                    {r.title}
                  </button>
                ))}
                {day.length > 2 && (
                  <button
                    onClick={() => setSelected(iso)}
                    className="px-1.5 text-left text-[11px] text-slate-400 hover:text-white"
                  >
                    +{day.length - 2} more
                  </button>
                )}
              </div>

              {/* Mobile: the whole day is one tap target; dots just signal what's there. */}
              {day.length > 0 && (
                <button
                  onClick={() => setSelected(iso)}
                  aria-label={`${day.length} release${day.length > 1 ? 's' : ''} on ${iso}`}
                  className="flex w-full flex-wrap justify-center gap-1 py-0.5 sm:hidden"
                >
                  {day.slice(0, 3).map((r) => (
                    <span key={r.id} className={`h-1.5 w-1.5 rounded-full ${dotTone[r.type]}`} />
                  ))}
                </button>
              )}
            </div>
          );
        })}
      </div>

      <Modal
        open={selected !== null}
        onClose={() => setSelected(null)}
        title={selected ? formatReleaseDate(selected) : undefined}
      >
        <div className="flex flex-col gap-3">
          {selectedReleases.map((r) => (
            <ReleaseCard
              key={r.id}
              r={r}
              onOpen={() => navigate(`/releases/${r.id}`)}
              onEdit={() => navigate(`/releases/${r.id}/edit`)}
              onArchive={() => {
                setSelected(null);
                onArchive(r);
              }}
              showOpenLink
            />
          ))}
        </div>
      </Modal>
    </div>
  );
}
