import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { Artist } from '@/types';
import { Button, MenuItem, RowMenu, Toast } from '@/components';
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
  const { toast, toastVariant, showToast } = useToast();
  const [artists, setArtists] = useState<Artist[]>([]);
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true);
    try {
      setArtists(await api.artists.list());
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

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
      load();
    } catch (e) {
      // Concurrency safety net: a release/song could have been added since the list loaded.
      showToast(e instanceof ApiError ? e.message : 'Failed to delete artist.');
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

      {loading ? (
        <p className="text-slate-400">Loading…</p>
      ) : artists.length === 0 ? (
        <p className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center text-slate-400">
          No artists yet. Add one to start creating releases.
        </p>
      ) : (
        <div className="overflow-hidden rounded-xl border border-edge bg-panel">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-edge text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Releases</th>
                <th className="px-4 py-3 font-medium">Songs</th>
                <th className="px-4 py-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {artists.map((a) => (
                <tr
                  key={a.id}
                  onClick={() => navigate(`/artists/${a.id}`)}
                  className="cursor-pointer border-b border-edge/50 last:border-b-0 hover:bg-edge/40"
                >
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
                              remove(a);
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
            </tbody>
          </table>
        </div>
      )}

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
