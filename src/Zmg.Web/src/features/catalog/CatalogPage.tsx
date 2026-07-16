import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { Artist, SongListItem } from '@/types';
import { Button, Toast, inputClass } from '@/components';
import { useConfirm } from '@/hooks/useConfirm';
import { useToast } from '@/hooks/useToast';

/**
 * The catalog (M13): every song, searchable by title, ordered by title. Release Date is derived
 * (earliest non-archived linked release, blank for orphans). Rows link into the song detail.
 * M15 adds the "Archived Songs →" link and per-row actions: Archive (canArchive) or Delete (orphan).
 */
export default function CatalogPage() {
  const navigate = useNavigate();
  const confirm = useConfirm();
  const { toast, toastVariant, showToast } = useToast();
  const [songs, setSongs] = useState<SongListItem[]>([]);
  const [artists, setArtists] = useState<Artist[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [q, setQ] = useState('');
  const [artistId, setArtistId] = useState('');

  function load() {
    setLoading(true);
    setError(null);
    api.songs
      .list({ q: q.trim() || undefined, artistId: artistId || undefined })
      .then(setSongs)
      .catch((e) => setError(e instanceof ApiError ? e.message : 'Failed to load catalog.'))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    api.artists.list().then(setArtists).catch(() => setError('Failed to load artists.'));
  }, []);

  useEffect(() => {
    const t = setTimeout(load, q ? 250 : 0);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [q, artistId]);

  const hasFilters = useMemo(() => q || artistId, [q, artistId]);

  async function archive(s: SongListItem) {
    if (
      !(await confirm({
        title: `Archive "${s.title}"?`,
        body: <p>Archived songs are read-only and can't be restored.</p>,
        confirmLabel: 'Archive',
        confirmVariant: 'archive',
      }))
    )
      return;
    try {
      await api.songs.archive(s.id);
      load();
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Failed to archive.');
    }
  }

  async function remove(s: SongListItem) {
    if (
      !(await confirm({
        title: `Delete "${s.title}" from the catalog?`,
        body: <p>This song was never released. This can't be undone.</p>,
        confirmLabel: 'Delete',
        confirmVariant: 'danger',
      }))
    )
      return;
    try {
      await api.songs.delete(s.id);
      load();
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : 'Failed to delete.');
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

      <div className="mb-5 flex flex-wrap items-center gap-3">
        <input
          className={`${inputClass} max-w-[16rem]`}
          placeholder="Search by title…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
        <select className={`${inputClass} max-w-[12rem]`} value={artistId} onChange={(e) => setArtistId(e.target.value)}>
          <option value="">All artists</option>
          {artists.map((a) => (
            <option key={a.id} value={a.id}>
              {a.name}
            </option>
          ))}
        </select>
        {hasFilters && (
          <Button
            variant="ghost"
            onClick={() => {
              setQ('');
              setArtistId('');
            }}
          >
            Clear
          </Button>
        )}
        <Link to="/catalog/archived" className="ml-auto shrink-0 text-sm text-slate-400 hover:text-accent">
          Archived Songs →
        </Link>
      </div>

      {error && <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>}

      {loading ? (
        <p className="text-slate-400">Loading…</p>
      ) : songs.length === 0 ? (
        <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center text-slate-400">
          {hasFilters ? 'No songs match these filters.' : 'No songs yet — add one or create a release.'}
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
                        variant="archive"
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

      <Toast message={toast} variant={toastVariant} />
    </div>
  );
}
