import { useEffect, useState } from 'react';
import { api, ApiError } from '@/api';
import type { Artist } from '@/types';
import { Button } from '@/components';
import { ArtistForm } from './components/ArtistForm';

export default function ArtistsPage() {
  const [artists, setArtists] = useState<Artist[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<Artist | null>(null);
  const [showForm, setShowForm] = useState(false);

  async function load() {
    setLoading(true);
    try {
      setArtists(await api.listArtists());
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function remove(a: Artist) {
    if (!confirm(`Delete artist "${a.name}"?`)) return;
    try {
      await api.deleteArtist(a.id);
      load();
    } catch (e) {
      alert(e instanceof ApiError ? e.message : 'Failed to delete artist.');
    }
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-white">Artists</h1>
          <p className="text-sm text-slate-400">Everyone with releases in the roster.</p>
        </div>
        <Button
          onClick={() => {
            setEditing(null);
            setShowForm(true);
          }}
        >
          + New artist
        </Button>
      </div>

      {showForm && (
        <ArtistForm
          artist={editing}
          onClose={() => setShowForm(false)}
          onSaved={() => {
            setShowForm(false);
            load();
          }}
        />
      )}

      {loading ? (
        <p className="text-slate-400">Loading…</p>
      ) : artists.length === 0 ? (
        <p className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center text-slate-400">
          No artists yet. Add one to start creating releases.
        </p>
      ) : (
        <div className="divide-y divide-edge overflow-hidden rounded-xl border border-edge bg-panel">
          {artists.map((a) => (
            <div key={a.id} className="flex flex-col gap-3 px-4 py-3 sm:flex-row sm:items-center sm:gap-4">
              <div className="min-w-0 flex-1">
                <p className="font-medium text-white">{a.name}</p>
                {a.notes && <p className="text-sm text-slate-400">{a.notes}</p>}
              </div>
              <div className="flex items-center gap-3">
                <span className="text-xs text-slate-500">
                  {a.releaseCount} release{a.releaseCount === 1 ? '' : 's'}
                </span>
                <Button
                  variant="ghost"
                  onClick={() => {
                    setEditing(a);
                    setShowForm(true);
                  }}
                >
                  Edit
                </Button>
                <Button variant="danger" onClick={() => remove(a)}>
                  Delete
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
