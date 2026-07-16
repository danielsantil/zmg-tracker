import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { SongListItem } from '@/types';
import { Button, inputClass } from '@/components';

/**
 * The catalog (M13): every song, searchable by title, ordered by title. Release Date is derived
 * (earliest non-archived linked release, blank for orphans). Rows link into the song detail.
 * M15 adds the "Archived Songs →" link and per-row actions: Archive (canArchive) or Delete (orphan).
 */
export default function CatalogPage() {
  const navigate = useNavigate();
  const [songs, setSongs] = useState<SongListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [q, setQ] = useState('');

  function load() {
    setLoading(true);
    setError(null);
    api.songs
      .list({ q: q.trim() || undefined })
      .then(setSongs)
      .catch((e) => setError(e instanceof ApiError ? e.message : 'Failed to load catalog.'))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    const t = setTimeout(load, q ? 250 : 0);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [q]);

  async function archive(s: SongListItem) {
    if (!confirm(`Archive "${s.title}"? Archived songs are read-only and can't be restored.`)) return;
    try {
      await api.songs.archive(s.id);
      load();
    } catch (e) {
      alert(e instanceof ApiError ? e.message : 'Failed to archive.');
    }
  }

  async function remove(s: SongListItem) {
    if (!confirm(`This song was never released — delete it from the catalog? This can't be undone.`)) return;
    try {
      await api.songs.delete(s.id);
      load();
    } catch (e) {
      alert(e instanceof ApiError ? e.message : 'Failed to delete.');
    }
  }

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">Catalog</h1>
          <p className="text-sm text-slate-400">Every song, by title.</p>
        </div>
        <Button onClick={() => navigate('/catalog/new')}>+ New song</Button>
      </div>

      <div className="mb-5 flex items-center justify-between gap-3">
        <input
          className={`${inputClass} max-w-[16rem]`}
          placeholder="Search by title…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
        <Link to="/catalog/archived" className="shrink-0 text-sm text-slate-400 hover:text-accent">
          Archived Songs →
        </Link>
      </div>

      {error && <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>}

      {loading ? (
        <p className="text-slate-400">Loading…</p>
      ) : songs.length === 0 ? (
        <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center text-slate-400">
          {q.trim() ? 'No songs match this search.' : 'No songs yet — add one or create a release.'}
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
                  <td className="px-4 py-3 font-medium text-white">{s.title}</td>
                  <td className="px-4 py-3 text-slate-300">{s.mainArtistName}</td>
                  <td className="px-4 py-3">
                    {s.isOrphan ? (
                      <Button
                        variant="danger"
                        onClick={(e) => {
                          e.stopPropagation();
                          remove(s);
                        }}
                      >
                        Delete
                      </Button>
                    ) : s.canArchive ? (
                      <Button
                        onClick={(e) => {
                          e.stopPropagation();
                          archive(s);
                        }}
                      >
                        Archive
                      </Button>
                    ) : (
                      <span className="text-xs text-slate-600">—</span>
                    )}
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
