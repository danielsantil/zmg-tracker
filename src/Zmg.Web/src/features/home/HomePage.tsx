import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { Artist, PendingAction, ReleaseListItem } from '@/types';
import { ReleaseType } from '@/types';
import { Button, inputClass } from '@/components';
import { PendingSection } from './components/PendingSection';
import { ReleaseCard } from './components/ReleaseCard';
import { EmptyState } from './components/EmptyState';

export default function HomePage() {
  const navigate = useNavigate();
  const [releases, setReleases] = useState<ReleaseListItem[]>([]);
  const [artists, setArtists] = useState<Artist[]>([]);
  const [pending, setPending] = useState<PendingAction[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [artistId, setArtistId] = useState('');
  const [type, setType] = useState('');
  const [status, setStatus] = useState('');

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [rels, arts, pend] = await Promise.all([
        api.releases.list({
          scope: 'home',
          artistId: artistId || undefined,
          type: type === '' ? undefined : (Number(type) as ReleaseType),
          status: status || undefined,
        }),
        api.artists.list(),
        api.pending.list(),
      ]);
      setReleases(rels);
      setArtists(arts);
      setPending(pend);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Failed to load home.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [artistId, type, status]);

  const hasFilters = useMemo(() => artistId || type || status, [artistId, type, status]);

  async function archive(r: ReleaseListItem) {
    if (!confirm(`Archive "${r.title}"? Archived releases are read-only and can't be restored.`)) return;
    try {
      await api.releases.archive(r.id);
      load();
    } catch (e) {
      alert(e instanceof ApiError ? e.message : 'Failed to archive.');
    }
  }

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">Home</h1>
          <p className="text-sm text-slate-400">Upcoming releases and what needs your attention.</p>
        </div>
        <Button onClick={() => navigate('/releases/new')}>+ New release</Button>
      </div>

      <PendingSection pending={pending} />

      <div className="mb-5 flex flex-wrap gap-3">
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
        <select className={`${inputClass} max-w-[10rem]`} value={status} onChange={(e) => setStatus(e.target.value)}>
          <option value="">All statuses</option>
          <option value="Upcoming">Upcoming</option>
          <option value="Complete">Complete</option>
        </select>
        {hasFilters && (
          <Button
            variant="ghost"
            onClick={() => {
              setArtistId('');
              setType('');
              setStatus('');
            }}
          >
            Clear
          </Button>
        )}
      </div>

      {error && <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>}

      {loading ? (
        <p className="text-slate-400">Loading…</p>
      ) : releases.length === 0 ? (
        <EmptyState hasArtists={artists.length > 0} />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {releases.map((r) => (
            <ReleaseCard
              key={r.id}
              r={r}
              onOpen={() => navigate(`/releases/${r.id}`)}
              onEdit={() => navigate(`/releases/${r.id}/edit`)}
              onArchive={() => archive(r)}
            />
          ))}
        </div>
      )}
    </div>
  );
}
