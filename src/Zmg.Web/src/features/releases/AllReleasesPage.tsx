import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { Artist, ReleaseListItem } from '@/types';
import { ReleaseType } from '@/types';
import { Button, IdentifierWarning, StatusBadge, TypeBadge, inputClass } from '@/components';
import { todayIso } from '@/lib/format';

export default function AllReleasesPage() {
  const navigate = useNavigate();
  const [releases, setReleases] = useState<ReleaseListItem[]>([]);
  const [artists, setArtists] = useState<Artist[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [artistId, setArtistId] = useState('');
  const [type, setType] = useState('');
  const [q, setQ] = useState('');
  const today = todayIso();

  async function loadReleases() {
    setLoading(true);
    setError(null);
    api.releases.list({
      scope: 'all',
      artistId: artistId || undefined,
      type: type === '' ? undefined : (Number(type) as ReleaseType),
      q: q.trim() || undefined,
    }).then(setReleases).catch((e) => setError(e instanceof ApiError ? e.message : 'Failed to load releases.'))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    api.artists.list().then(setArtists).catch(() => setError('Failed to load artists.'));
  }, []);

  // Debounce the free-text search so each keystroke doesn't fire a request.
  useEffect(() => {
    const t = setTimeout(loadReleases, q ? 250 : 0);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [artistId, type, q]);

  const hasFilters = useMemo(() => artistId || type || q, [artistId, type, q]);

  async function archive(r: ReleaseListItem) {
    if (!confirm(`Archive "${r.title}"? Archived releases are read-only and can't be restored.`)) return;
    try {
      await api.releases.archive(r.id);
      loadReleases();
    } catch (e) {
      alert(e instanceof ApiError ? e.message : 'Failed to archive.');
    }
  }

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">All Releases</h1>
          <p className="text-sm text-slate-400">Every release, newest first.</p>
        </div>
        <Button onClick={() => navigate('/releases/new')}>+ New release</Button>
      </div>

      <div className="mb-5 flex flex-wrap gap-3">
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
        <select className={`${inputClass} max-w-[10rem]`} value={type} onChange={(e) => setType(e.target.value)}>
          <option value="">All types</option>
          <option value="0">Single</option>
          <option value="1">Album</option>
        </select>
        {hasFilters && (
          <Button
            variant="ghost"
            onClick={() => {
              setArtistId('');
              setType('');
              setQ('');
            }}
          >
            Clear
          </Button>
        )}
      </div>

      {/* Kept outside the table so it stays reachable even when there are no releases. */}
      <div className="mb-3 flex justify-end">
        <Link to="/releases/archived" className="text-sm text-slate-400 hover:text-accent">
          Archived Releases →
        </Link>
      </div>

      {error && <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>}

      {loading ? (
        <p className="text-slate-400">Loading…</p>
      ) : releases.length === 0 ? (
        <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center text-slate-400">
          {hasFilters ? 'No releases match these filters.' : 'No releases yet.'}
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-edge bg-panel">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-edge text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Type</th>
                <th className="px-4 py-3 font-medium">Released Date</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Action</th>
              </tr>
            </thead>
            <tbody>
              {releases.map((r) => (
                <tr
                  key={r.id}
                  onClick={() => navigate(`/releases/${r.id}`)}
                  className="cursor-pointer border-b border-edge/50 last:border-b-0 hover:bg-edge/40"
                >
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1.5">
                      <Link
                        to={`/releases/${r.id}`}
                        onClick={(e) => e.stopPropagation()}
                        className="font-medium text-white hover:text-accent"
                      >
                        {r.title}
                      </Link>
                      {r.needsIdentifierWarning && <IdentifierWarning />}
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
                    {/* Archive is only allowed for releases still to come (releaseDate >= today). */}
                    {r.releaseDate >= today && (
                      <Button
                        variant="ghost"
                        onClick={(e) => {
                          e.stopPropagation();
                          archive(r);
                        }}
                      >
                        Archive
                      </Button>
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
