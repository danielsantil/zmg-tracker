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
    const activeDependents = a.releaseCount + a.songCount + a.creditCount;
    if (activeDependents > 0) {
      const parts = [
        a.releaseCount > 0 ? `${a.releaseCount} release${a.releaseCount === 1 ? '' : 's'}` : null,
        a.songCount > 0 ? `${a.songCount} song${a.songCount === 1 ? '' : 's'}` : null,
        a.creditCount > 0 ? `${a.creditCount} feat/collab credit${a.creditCount === 1 ? '' : 's'}` : null,
      ].filter(Boolean);
      await confirm({
        title: `Can't delete "${a.name}"`,
        body: <p>This artist is tied to {parts.join(', ')}.</p>,
        confirmLabel: 'OK',
        hideCancel: true,
      });
      return;
    }

    // No active ties, but archived data may reference the artist — warn that deleting the artist
    // permanently removes that archived data too (the server cascades it in the same delete).
    const archivedParts = [
      a.archivedReleaseCount > 0
        ? `${a.archivedReleaseCount} archived release${a.archivedReleaseCount === 1 ? '' : 's'}`
        : null,
      a.archivedSongCount > 0
        ? `${a.archivedSongCount} archived song${a.archivedSongCount === 1 ? '' : 's'}`
        : null,
    ].filter(Boolean);

    if (
      !(await confirm({
        title: `Delete artist "${a.name}"?`,
        body:
          archivedParts.length > 0 ? (
            <p>
              This artist has {archivedParts.join(' and ')} that will also be permanently removed. This
              can't be undone.
            </p>
          ) : (
            <p>This can't be undone.</p>
          ),
        confirmLabel: 'Delete',
        confirmVariant: 'danger',
      }))
    )
      return;
    try {
      await api.artists.delete(a.id);
      void queryClient.invalidateQueries({ queryKey: queryKeys.artists });
      void queryClient.invalidateQueries({ queryKey: queryKeys.songs() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.releases() });
    } catch (e) {
      // Concurrency safety net: a release/song could have been added since the list loaded.
      showToast(errorMessage(e, 'Failed to delete artist.'));
    }
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-strong">Artists</h1>
          <p className="text-sm text-muted">Everyone with releases in the roster.</p>
        </div>
        <Button onClick={() => navigate('/artists/new')}>+ New artist</Button>
      </div>

      {isLoading ? (
        <Loading />
      ) : artists.length === 0 ? (
        <EmptyState>No artists yet. Add one to start creating releases.</EmptyState>
      ) : (
        <DataTable
          headers={[
            { label: 'Name' },
            { label: 'Releases' },
            { label: 'Songs' },
            { label: 'Collabs' },
            { label: '', className: 'text-right' },
          ]}
        >
          {artists.map((a) => (
            <tr key={a.id} onClick={() => navigate(`/artists/${a.id}`)} className={dataRowClass}>
              <td className="px-4 py-3">
                <Link
                  to={`/artists/${a.id}`}
                  onClick={(e) => e.stopPropagation()}
                  className="font-medium text-strong hover:text-accent"
                >
                  {a.name}
                </Link>
                {a.notes && <p className="text-xs text-subtle">{a.notes}</p>}
              </td>
              <td className="px-4 py-3 text-body">{a.releaseCount}</td>
              <td className="px-4 py-3 text-body">{a.songCount}</td>
              <td className="px-4 py-3 text-body">{a.creditCount}</td>
              <td className="px-4 py-3 text-right">
                <div onClick={(e) => e.stopPropagation()} className="flex justify-end">
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
