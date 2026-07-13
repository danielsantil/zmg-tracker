import { useState } from 'react';
import { api, ApiError } from '@/api';
import type { Artist } from '@/types';
import { Button, Field, inputClass } from '@/components';

export function ArtistForm({
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
      if (artist) await api.artists.update(artist.id, input);
      else await api.artists.create(input);
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
