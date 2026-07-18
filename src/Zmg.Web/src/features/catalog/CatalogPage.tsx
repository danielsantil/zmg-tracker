import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '@/api';
import { useSongs, useArtists, queryKeys } from '@/api/queries';
import type { SongListItem } from '@/types';
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
  Toast,
} from '@/components';
import { useToast } from '@/hooks/useToast';
import { useConfirmDelete } from '@/hooks/useConfirmDelete';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';

/**
 * The catalog (M13): every song, searchable by title, ordered by title. Release Date is derived
 * (earliest non-archived linked release, blank for orphans). Rows link into the song detail.
 * M15 adds the "Archived Songs →" link and per-row actions: Archive (canArchive) or Delete (orphan).
 */
export default function CatalogPage() {
  const navigate = useNavigate();
  const { toast, toastVariant, showToast } = useToast();
  const [q, setQ] = useState('');
  const [artistId, setArtistId] = useState('');
  const debouncedQ = useDebouncedValue(q);

  const { data: artists = [] } = useArtists();
  const { data: songs = [], isLoading, error } = useSongs({
    q: debouncedQ.trim() || undefined,
    artistId: artistId || undefined,
  });

  const hasFilters = !!(q || artistId);

  const archive = useConfirmDelete<SongListItem>({
    confirm: (s) => ({
      title: `Archive "${s.title}"?`,
      body: <p>Archived songs are read-only and can't be restored.</p>,
      confirmLabel: 'Archive',
      confirmVariant: 'archive',
    }),
    mutate: (s) => api.songs.archive(s.id),
    invalidate: [queryKeys.songs()],
    errorFallback: 'Failed to archive.',
    showToast,
  });

  const remove = useConfirmDelete<SongListItem>({
    confirm: (s) => ({
      title: `Delete "${s.title}" from the catalog?`,
      body: <p>This song was never released. This can't be undone.</p>,
      confirmLabel: 'Delete',
      confirmVariant: 'danger',
    }),
    mutate: (s) => api.songs.delete(s.id),
    invalidate: [queryKeys.songs()],
    errorFallback: 'Failed to delete.',
    showToast,
  });

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-strong">Catalog</h1>
          <p className="text-sm text-muted">Every song, by title.</p>
        </div>
        <Button onClick={() => navigate('/catalog/new')}>+ New song</Button>
      </div>

      <FilterBar
        onClear={hasFilters ? () => { setQ(''); setArtistId(''); } : undefined}
        trailing={
          <Link to="/catalog/archived" className="ml-auto shrink-0 text-sm text-muted hover:text-accent">
            Archived Songs →
          </Link>
        }
      >
        <SearchInput value={q} onChange={setQ} />
        <ArtistSelect artists={artists} value={artistId} onChange={setArtistId} />
      </FilterBar>

      <ErrorBanner error={error ? 'Failed to load catalog.' : null} />

      {isLoading ? (
        <Loading />
      ) : songs.length === 0 ? (
        <EmptyState>
          {hasFilters ? 'No songs match these filters.' : 'No songs yet — add one or create a release.'}
        </EmptyState>
      ) : (
        <DataTable
          headers={[
            { label: 'Name' },
            { label: 'Main Artist' },
            { label: 'Released' },
            { label: '', className: 'text-right' },
          ]}
        >
          {songs.map((s) => (
            <tr key={s.id} onClick={() => navigate(`/catalog/${s.id}`)} className={dataRowClass}>
              <td className="px-4 py-3 font-medium text-strong">{s.title}</td>
              <td className="px-4 py-3 text-body">{s.mainArtistName}</td>
              <td className="px-4 py-3">
                {s.releaseCount > 0 ? <span className="text-green-400">Yes</span> : <span className="text-muted">No</span>}
              </td>
              <td className="px-4 py-3 text-right">
                {(s.isOrphan || s.canArchive) && (
                  <div onClick={(e) => e.stopPropagation()} className="flex justify-end">
                    <RowMenu label="Song actions">
                      {(close) =>
                        s.isOrphan ? (
                          <MenuItem tone="danger" onClick={() => { close(); void remove(s); }}>
                            Delete
                          </MenuItem>
                        ) : (
                          <MenuItem tone="archive" onClick={() => { close(); void archive(s); }}>
                            Archive
                          </MenuItem>
                        )
                      }
                    </RowMenu>
                  </div>
                )}
              </td>
            </tr>
          ))}
        </DataTable>
      )}

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
