import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { SongListItem } from '@/types';
import { Button } from '@/components';

/**
 * Archived Songs (M15) — the terminal, read-only bucket, mirroring Archived Releases. Table is
 * Name · Main Artist · Action, where the action is Delete: a soft-delete on the server (songs are
 * never hard-deleted). Reached via the "Archived Songs →" link on the catalog, not a nav item.
 */
export default function ArchivedSongsPage() {
  const navigate = useNavigate();
  const [songs, setSongs] = useState<SongListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  function load() {
    setLoading(true);
    setError(null);
    api.songs
      .list({ scope: 'archived' })
      .then(setSongs)
      .catch((e) => setError(e instanceof ApiError ? e.message : 'Failed to load archived songs.'))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
  }, []);

  async function remove(s: SongListItem) {
    if (!confirm(`Delete "${s.title}"? This can't be undone.`)) return;
    try {
      await api.songs.delete(s.id);
      load();
    } catch (e) {
      alert(e instanceof ApiError ? e.message : 'Failed to delete.');
    }
  }

  return (
    <div>
      <div className="mb-6">
        <Link to="/catalog" className="text-sm text-slate-400 hover:text-slate-200">
          ‹ Catalog
        </Link>
        <h1 className="mt-2 text-2xl font-semibold text-white">Archived Songs</h1>
        <p className="text-sm text-slate-400">Archived songs are read-only and can't be restored.</p>
      </div>

      {error && <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>}

      {loading ? (
        <p className="text-slate-400">Loading…</p>
      ) : songs.length === 0 ? (
        <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center text-slate-400">
          No archived songs.
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-edge bg-panel">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-edge text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Main Artist</th>
                <th className="px-4 py-3 font-medium">Action</th>
              </tr>
            </thead>
            <tbody>
              {songs.map((s) => (
                <tr
                  key={s.id}
                  onClick={() => navigate(`/catalog/${s.id}`)}
                  className="cursor-pointer border-b border-edge/50 last:border-b-0 hover:bg-edge/40"
                >
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
                  <td className="px-4 py-3">
                    <Button
                      variant="danger"
                      onClick={(e) => {
                        e.stopPropagation();
                        remove(s);
                      }}
                    >
                      Delete
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
