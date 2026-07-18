import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '@/api';
import { useReleases, useArtists, queryKeys } from '@/api/queries';
import type { ReleaseListItem } from '@/types';
import { ReleaseType } from '@/types';
import {
  ArtistSelect,
  Button,
  DataTable,
  dataRowClass,
  EmptyState,
  ErrorBanner,
  FilterBar,
  Loading,
  MenuItem,
  RowMenu,
  SearchInput,
  SoftWarning,
  StatusBadge,
  StatusSelect,
  Toast,
  TypeBadge,
  TypeSelect,
} from '@/components';
import { usePersistedState } from '@/hooks/usePersistedState';
import { useToast } from '@/hooks/useToast';
import { useConfirmDelete } from '@/hooks/useConfirmDelete';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { archiveReleaseConfirm } from './archiveConfirm';
import { ReleaseCalendar } from './components/ReleaseCalendar';

const VIEWS = ['table', 'calendar'] as const;
type View = (typeof VIEWS)[number];
const isView = (v: unknown): v is View => VIEWS.includes(v as View);

export default function AllReleasesPage() {
  const navigate = useNavigate();
  const { toast, toastVariant, showToast } = useToast();

  // Sticky so the calendar doesn't silently revert to the table on every visit.
  const [view, setView] = usePersistedState<View>('releases.view', 'table', isView);
  const [artistId, setArtistId] = useState('');
  const [type, setType] = useState('');
  const [status, setStatus] = useState('');
  const [q, setQ] = useState('');
  const debouncedQ = useDebouncedValue(q);

  const { data: artists = [] } = useArtists();
  const { data: releases = [], isLoading, error } = useReleases({
    scope: 'all',
    artistId: artistId || undefined,
    type: type === '' ? undefined : (Number(type) as ReleaseType),
    status: status || undefined,
    q: debouncedQ.trim() || undefined,
  });

  const hasFilters = !!(artistId || type || status || q);

  const archive = useConfirmDelete<ReleaseListItem>({
    confirm: (r) => archiveReleaseConfirm(r.id, r.title),
    mutate: (r) => api.releases.archive(r.id),
    invalidate: [queryKeys.releases(), queryKeys.pending],
    errorFallback: 'Failed to archive.',
    showToast,
  });

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">Releases</h1>
          <p className="text-sm text-slate-400">Every release, newest first.</p>
        </div>
        <Button onClick={() => navigate('/releases/new')}>+ New release</Button>
      </div>

      <FilterBar onClear={hasFilters ? () => { setArtistId(''); setType(''); setStatus(''); setQ(''); } : undefined}>
        <SearchInput value={q} onChange={setQ} />
        <ArtistSelect artists={artists} value={artistId} onChange={setArtistId} />
        <TypeSelect value={type} onChange={setType} />
        <StatusSelect value={status} onChange={setStatus} options={['Upcoming', 'Released', 'Complete']} />
      </FilterBar>

      {/* Kept outside the table so both stay reachable even when there are no releases. */}
      <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
        <div className="inline-flex rounded-lg border border-edge bg-panel p-0.5">
          {VIEWS.map((v) => (
            <button
              key={v}
              onClick={() => setView(v)}
              aria-pressed={view === v}
              className={`rounded-md px-3 py-1 text-sm font-medium capitalize transition ${
                view === v ? 'bg-edge text-white' : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              {v}
            </button>
          ))}
        </div>
        <Link to="/releases/archived" className="text-sm text-slate-400 hover:text-accent">
          Archived Releases →
        </Link>
      </div>

      <ErrorBanner error={error ? 'Failed to load releases.' : null} />

      {isLoading ? (
        <Loading />
      ) : view === 'calendar' ? (
        /* The calendar reuses the fetched, already-filtered list — an empty month speaks for itself. */
        <ReleaseCalendar releases={releases} onArchive={archive} />
      ) : releases.length === 0 ? (
        <EmptyState>{hasFilters ? 'No releases match these filters.' : 'No releases yet.'}</EmptyState>
      ) : (
        <DataTable headers={['Name', 'Type', 'Released Date', 'Status', 'Action']}>
          {releases.map((r) => (
            <tr key={r.id} onClick={() => navigate(`/releases/${r.id}`)} className={dataRowClass}>
              <td className="px-4 py-3">
                <div className="flex items-center gap-1.5">
                  <Link
                    to={`/releases/${r.id}`}
                    onClick={(e) => e.stopPropagation()}
                    className="font-medium text-white hover:text-accent"
                  >
                    {r.title}
                  </Link>
                  <SoftWarning warnings={r.warnings} />
                </div>
                <div className="text-xs text-slate-400">{r.mainArtistName}</div>
              </td>
              <td className="px-4 py-3">
                <TypeBadge type={r.type} />
              </td>
              <td className="whitespace-nowrap px-4 py-3 text-slate-300">{r.releaseDate}</td>
              <td className="px-4 py-3">
                <StatusBadge status={r.status} />
              </td>
              <td className="px-4 py-3">
                <div onClick={(e) => e.stopPropagation()} className="w-fit">
                  <RowMenu label="Release actions">
                    {(close) => (
                      <>
                        <MenuItem onClick={() => { close(); void navigate(`/releases/${r.id}/edit`); }}>
                          Edit
                        </MenuItem>
                        {/* Archive affordance follows the server's canArchive (upcoming & not archived). */}
                        {r.canArchive && (
                          <MenuItem tone="archive" onClick={() => { close(); void archive(r); }}>
                            Archive
                          </MenuItem>
                        )}
                      </>
                    )}
                  </RowMenu>
                </div>
              </td>
            </tr>
          ))}
        </DataTable>
      )}

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
