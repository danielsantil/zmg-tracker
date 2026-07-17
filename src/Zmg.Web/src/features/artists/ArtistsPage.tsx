import { Link, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { api, errorMessage } from '@/api';
import { useArtists, queryKeys } from '@/api/queries';
import type { Artist } from '@/types';
import { Button, DataTable, dataRowClass, EmptyState, Loading, MenuItem, RowMenu, Toast } from '@/components';
import { useConfirm } from '@/hooks/useConfirm';
import { useToast } from '@/hooks/useToast';

/**
 * Artists roster (M19): a bordered table (Name · Releases · Songs · Actions) matching Catalog/Releases,
 * with a kebab per row. Delete checks the release/song counts the row already carries and branches
 * *before* asking — an info modal when the artist is still referenced, a red confirm when it's clean —
 * so the server-side guard never surfaces as a post-hoc error toast. Create/edit live on dedicated pages.
 */
export default function ArtistsPage() {
  const navigate = useNavigate();
  const confirm = useConfirm();
  const queryClient = useQueryClient();
  const { toast, toastVariant, showToast } = useToast();
  const { data: artists = [], isLoading } = useArtists();

  async function remove(a: Artist) {
    const dependents = a.releaseCount + a.songCount + a.creditCount;
    if (dependents > 0) {
      const parts = [
        a.releaseCount > 0 ? `${a.releaseCount} release${a.releaseCount === 1 ? '' : 's'}` : null,
        a.songCount > 0 ? `${a.songCount} song${a.songCount === 1 ? '' : 's'}` : null,
        a.creditCount > 0 ? `${a.creditCount} feat/collab credit${a.creditCount === 1 ? '' : 's'}` : null,
      ].filter(Boolean);
      await confirm({
        title: `Can't delete "${a.name}"`,
        body: <p>This artist is still tied to {parts.join(', ')}. Remove those first.</p>,
        confirmLabel: 'OK',
        hideCancel: true,
      });
      return;
    }

    if (
      !(await confirm({
        title: `Delete artist "${a.name}"?`,
        body: <p>This can't be undone.</p>,
        confirmLabel: 'Delete',
        confirmVariant: 'danger',
      }))
    )
      return;
    try {
      await api.artists.delete(a.id);
      void queryClient.invalidateQueries({ queryKey: queryKeys.artists });
    } catch (e) {
      // Concurrency safety net: a release/song could have been added since the list loaded.
      showToast(errorMessage(e, 'Failed to delete artist.'));
    }
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-white">Artists</h1>
          <p className="text-sm text-slate-400">Everyone with releases in the roster.</p>
        </div>
        <Button onClick={() => navigate('/artists/new')}>+ New artist</Button>
      </div>

      {isLoading ? (
        <Loading />
      ) : artists.length === 0 ? (
        <EmptyState>No artists yet. Add one to start creating releases.</EmptyState>
      ) : (
        <DataTable headers={['Name', 'Releases', 'Songs', 'Actions']}>
          {artists.map((a) => (
            <tr key={a.id} onClick={() => navigate(`/artists/${a.id}`)} className={dataRowClass}>
              <td className="px-4 py-3">
                <Link
                  to={`/artists/${a.id}`}
                  onClick={(e) => e.stopPropagation()}
                  className="font-medium text-white hover:text-accent"
                >
                  {a.name}
                </Link>
                {a.notes && <p className="text-xs text-slate-500">{a.notes}</p>}
              </td>
              <td className="px-4 py-3 text-slate-300">{a.releaseCount}</td>
              <td className="px-4 py-3 text-slate-300">{a.songCount}</td>
              <td className="px-4 py-3">
                <div onClick={(e) => e.stopPropagation()} className="w-fit">
                  <RowMenu label="Artist actions">
                    {(close) => (
                      <MenuItem
                        tone="danger"
                        onClick={() => {
                          close();
                          void remove(a);
                        }}
                      >
                        Delete
                      </MenuItem>
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
