import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '../api';
import type { Artist, ReleaseListItem } from '../types';
import { ReleaseType } from '../types';
import { Button, IdentifierWarning, ProgressBar, StatusBadge, TypeBadge, daysToRelease, inputClass } from '../ui';

export default function Home() {
  const navigate = useNavigate();
  const [releases, setReleases] = useState<ReleaseListItem[]>([]);
  const [artists, setArtists] = useState<Artist[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [artistId, setArtistId] = useState('');
  const [type, setType] = useState('');
  const [status, setStatus] = useState('');

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [rels, arts] = await Promise.all([
        api.listReleases({
          scope: 'home',
          artistId: artistId || undefined,
          type: type === '' ? undefined : (Number(type) as ReleaseType),
          status: status || undefined,
        }),
        api.listArtists(),
      ]);
      setReleases(rels);
      setArtists(arts);
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

  async function remove(r: ReleaseListItem) {
    if (!confirm(`Delete "${r.title}"? This removes its checklist.`)) return;
    try {
      await api.deleteRelease(r.id);
      load();
    } catch (e) {
      alert(e instanceof ApiError ? e.message : 'Failed to delete.');
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

      {/* Pending Tasks section slot — filled in M10. */}

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
          <option value="Released">Released</option>
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
              onDelete={() => remove(r)}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function ReleaseCard({
  r,
  onOpen,
  onEdit,
  onDelete,
}: {
  r: ReleaseListItem;
  onOpen: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const days = daysToRelease(r.releaseDate);
  const countdown =
    r.status === 'Upcoming' && days >= 0 ? (days === 0 ? 'Releases today' : `${days} days to release`) : null;

  return (
    <div className="flex flex-col overflow-hidden rounded-xl border border-edge bg-panel">
      <button onClick={onOpen} className="block aspect-[16/9] w-full bg-edge text-left">
        {r.coverUrl ? (
          <img src={r.coverUrl} alt="" className="h-full w-full object-cover" />
        ) : (
          <div className="grid h-full place-items-center text-3xl font-semibold text-slate-600">
            {r.title.slice(0, 1).toUpperCase()}
          </div>
        )}
      </button>
      <div className="flex flex-1 flex-col gap-3 p-4">
        <div>
          <div className="flex items-start justify-between gap-2">
            <button onClick={onOpen} className="text-left font-semibold text-white hover:text-accent">
              {r.title}
            </button>
            <div className="flex items-center gap-1.5">
              {r.needsIdentifierWarning && <IdentifierWarning upc={r.upc} isrc={r.isrc} />}
              <StatusBadge status={r.status} />
            </div>
          </div>
          <p className="text-sm text-slate-400">{r.mainArtistName}</p>
        </div>
        <div className="flex items-center gap-2 text-xs text-slate-400">
          <TypeBadge type={r.type} />
          <span>{r.releaseDate}</span>
          {countdown && <span className="text-accent">· {countdown}</span>}
        </div>
        <div className="mt-auto">
          <ProgressBar done={r.doneTasks} total={r.totalTasks} />
        </div>
        <div className="flex gap-2">
          <Button variant="ghost" onClick={onEdit}>
            Edit
          </Button>
          <Button variant="danger" onClick={onDelete}>
            Delete
          </Button>
        </div>
      </div>
    </div>
  );
}

function EmptyState({ hasArtists }: { hasArtists: boolean }) {
  return (
    <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center">
      <p className="text-slate-300">No upcoming releases.</p>
      <p className="mt-1 text-sm text-slate-500">
        {hasArtists ? (
          <>
            Create one from the{' '}
            <Link to="/releases/new" className="text-accent underline">
              New release
            </Link>{' '}
            form, or browse{' '}
            <Link to="/releases" className="text-accent underline">
              All Releases
            </Link>
            .
          </>
        ) : (
          <>
            Start by adding an artist on the{' '}
            <Link to="/artists" className="text-accent underline">
              Artists
            </Link>{' '}
            page.
          </>
        )}
      </p>
    </div>
  );
}
