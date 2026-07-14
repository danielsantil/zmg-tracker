import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { ReleaseListItem } from '@/types';
import { Button, StatusBadge, TypeBadge } from '@/components';

/**
 * Archived Releases (v1.2) — the terminal, read-only bucket. Same table design as All Releases
 * (Name · Type · Released Date · Action), but the action is Remove: a soft-delete on the server
 * (releases are never hard-deleted). Reached via the "Archived Releases →" link on All Releases,
 * not a nav item.
 */
export default function ArchivedReleasesPage() {
  const navigate = useNavigate();
  const [releases, setReleases] = useState<ReleaseListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  function load() {
    setLoading(true);
    setError(null);
    api.releases
      .list({ scope: 'archived' })
      .then(setReleases)
      .catch((e) => setError(e instanceof ApiError ? e.message : 'Failed to load archived releases.'))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
  }, []);

  async function remove(r: ReleaseListItem) {
    if (!confirm(`Delete "${r.title}"? This can't be undone.`)) return;
    try {
      await api.releases.delete(r.id);
      load();
    } catch (e) {
      alert(e instanceof ApiError ? e.message : 'Failed to delete.');
    }
  }

  return (
    <div>
      <div className="mb-6">
        <Link to="/releases" className="text-sm text-slate-400 hover:text-slate-200">
          ‹ All Releases
        </Link>
        <h1 className="mt-2 text-2xl font-semibold text-white">Archived Releases</h1>
        <p className="text-sm text-slate-400">Archived releases are read-only and can't be restored.</p>
      </div>

      {error && <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>}

      {loading ? (
        <p className="text-slate-400">Loading…</p>
      ) : releases.length === 0 ? (
        <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center text-slate-400">
          No archived releases.
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-edge bg-panel">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-edge text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Type</th>
                <th className="px-4 py-3 font-medium">Released Date</th>
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
                      <StatusBadge status={r.status} />
                    </div>
                    <div className="text-xs text-slate-400">{r.mainArtistName}</div>
                  </td>
                  <td className="px-4 py-3">
                    <TypeBadge type={r.type} />
                  </td>
                  <td className="whitespace-nowrap px-4 py-3 text-slate-300">{r.releaseDate}</td>
                  <td className="px-4 py-3">
                    <Button
                      variant="danger"
                      onClick={(e) => {
                        e.stopPropagation();
                        remove(r);
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
