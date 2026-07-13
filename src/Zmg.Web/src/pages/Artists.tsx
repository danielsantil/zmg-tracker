import { useEffect, useState } from 'react';
import { api, ApiError } from '@/api';
import type { Artist } from '@/types';
import { Button, Field, inputClass } from '@/components';

export default function Artists() {
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

function ArtistForm({
  artist,
  onClose,
  onSaved,
}: {
  artist: Artist | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [name, setName] = useState(artist?.name ?? '');
  const [notes, setNotes] = useState(artist?.notes ?? '');
  const [errors, setErrors] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors([]);
    try {
      const input = { name, notes: notes || null };
      if (artist) await api.updateArtist(artist.id, input);
      else await api.createArtist(input);
      onSaved();
    } catch (err) {
      setErrors(err instanceof ApiError ? err.errors : ['Failed to save artist.']);
    } finally {
      setSaving(false);
    }
  }

  return (
    <form onSubmit={submit} className="mb-6 space-y-4 rounded-xl border border-edge bg-panel p-4">
      <h2 className="font-semibold text-white">{artist ? 'Edit artist' : 'New artist'}</h2>
      {errors.length > 0 && (
        <ul className="rounded-lg bg-red-500/10 px-4 py-2 text-sm text-red-300">
          {errors.map((msg) => (
            <li key={msg}>{msg}</li>
          ))}
        </ul>
      )}
      <Field label="Name">
        <input className={inputClass} value={name} onChange={(e) => setName(e.target.value)} autoFocus />
      </Field>
      <Field label="Notes" hint="Optional">
        <textarea className={inputClass} rows={2} value={notes} onChange={(e) => setNotes(e.target.value)} />
      </Field>
      <div className="flex gap-2">
        <Button type="submit" disabled={saving}>
          {saving ? 'Saving…' : 'Save'}
        </Button>
        <Button type="button" variant="ghost" onClick={onClose}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
