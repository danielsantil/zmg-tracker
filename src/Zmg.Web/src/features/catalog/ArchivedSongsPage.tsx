import { Link, useNavigate } from 'react-router-dom';
import { api } from '@/api';
import { useSongs, queryKeys } from '@/api/queries';
import type { SongListItem } from '@/types';
import { Button, DataTable, dataRowClass, EmptyState, ErrorBanner, Loading, Toast } from '@/components';
import { useToast } from '@/hooks/useToast';
import { useConfirmDelete } from '@/hooks/useConfirmDelete';

/**
 * Archived Songs (M15) — the terminal, read-only bucket, mirroring Archived Releases. Table is
 * Name · Main Artist · Action, where the action is Delete: a soft-delete on the server (songs are
 * never hard-deleted). Reached via the "Archived Songs →" link on the catalog, not a nav item.
 */
export default function ArchivedSongsPage() {
  const navigate = useNavigate();
  const { toast, toastVariant, showToast } = useToast();
  const { data: songs = [], isLoading, error } = useSongs({ scope: 'archived' });

  const remove = useConfirmDelete<SongListItem>({
    confirm: (s) => ({
      title: `Delete "${s.title}"?`,
      body: <p>This can't be undone.</p>,
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
      <div className="mb-6">
        <Link to="/catalog" className="text-sm text-slate-400 hover:text-slate-200">
          ‹ Catalog
        </Link>
        <h1 className="mt-2 text-2xl font-semibold text-white">Archived Songs</h1>
        <p className="text-sm text-slate-400">Archived songs are read-only and can't be restored.</p>
      </div>

      <ErrorBanner error={error ? 'Failed to load archived songs.' : null} />

      {isLoading ? (
        <Loading />
      ) : songs.length === 0 ? (
        <EmptyState>No archived songs.</EmptyState>
      ) : (
        <DataTable
          headers={[
            { label: 'Name' },
            { label: 'Main Artist' },
            { label: '', className: 'text-right' },
          ]}
        >
          {songs.map((s) => (
            <tr key={s.id} onClick={() => navigate(`/catalog/${s.id}`)} className={dataRowClass}>
              <td className="px-4 py-3">
                <Link
                  to={`/catalog/${s.id}`}
                  onClick={(e) => e.stopPropagation()}
                  className="font-medium text-white hover:text-accent"
                >
                  {s.title}
                </Link>
              </td>
              <td className="px-4 py-3 text-slate-300">{s.mainArtistName}</td>
              <td className="px-4 py-3 text-right">
                <Button
                  variant="danger"
                  onClick={(e) => {
                    e.stopPropagation();
                    void remove(s);
                  }}
                >
                  Delete
                </Button>
              </td>
            </tr>
          ))}
        </DataTable>
      )}

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
