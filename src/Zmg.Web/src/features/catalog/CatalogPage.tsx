import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { SongListItem } from '@/types';
import { inputClass } from '@/components';

/**
 * The catalog (M13): every song, searchable by title, ordered by title. Release Date is derived
 * (earliest non-archived linked release, blank for orphans). Rows link into the song detail.
 * The archived view + per-row actions land in M15.
 */
export default function CatalogPage() {
  const navigate = useNavigate();
  const [songs, setSongs] = useState<SongListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [q, setQ] = useState('');

  useEffect(() => {
    const t = setTimeout(() => {
      setLoading(true);
      setError(null);
      api.songs
        .list({ q: q.trim() || undefined })
        .then(setSongs)
        .catch((e) => setError(e instanceof ApiError ? e.message : 'Failed to load catalog.'))
        .finally(() => setLoading(false));
    }, q ? 250 : 0);
    return () => clearTimeout(t);
  }, [q]);

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-white">Catalog</h1>
        <p className="text-sm text-slate-400">Every song, by title.</p>
      </div>

      <div className="mb-5">
        <input
          className={`${inputClass} max-w-[16rem]`}
          placeholder="Search by title…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
      </div>

      {error && <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>}

      {loading ? (
        <p className="text-slate-400">Loading…</p>
      ) : songs.length === 0 ? (
        <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center text-slate-400">
          {q.trim() ? 'No songs match this search.' : 'No songs yet — create a release to add one.'}
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-edge bg-panel">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-edge text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Main Artist</th>
                <th className="px-4 py-3 font-medium">Release Date</th>
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
                  <td className="whitespace-nowrap px-4 py-3 text-slate-300">{s.releaseDate ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
