import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api, ApiError } from '@/api';
import { Button, Field, inputClass, inputErrorClass } from '@/components';
import { useBackNavigation } from '@/hooks/useBackNavigation';

/**
 * Create/edit an artist on a dedicated page (M19), mirroring SongFormPage and replacing the old
 * inline ArtistForm. No `:id` → create; with `:id` → load and prefill for edit. Leaves room for
 * future artist fields (DSP ids, etc.).
 */
export default function ArtistFormPage() {
  const navigate = useNavigate();
  const goBack = useBackNavigation();
  const { id } = useParams();
  const editing = Boolean(id);

  const [loading, setLoading] = useState(editing);
  const [errors, setErrors] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<{ name?: string }>({});

  const [name, setName] = useState('');
  const [notes, setNotes] = useState('');

  useEffect(() => {
    if (!id) return;
    (async () => {
      try {
        const artist = await api.artists.get(id);
        setName(artist.name);
        setNotes(artist.notes ?? '');
      } catch (err) {
        setErrors(err instanceof ApiError ? err.errors : ['Failed to load artist.']);
      } finally {
        setLoading(false);
      }
    })();
  }, [id]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErrors([]);
    setFieldErrors({});

    if (!name.trim()) {
      setFieldErrors({ name: 'Artist name is required.' });
      return;
    }

    setSaving(true);
    try {
      const input = { name: name.trim(), notes: notes.trim() || null };
      if (id) {
        await api.artists.update(id, input);
        goBack();
      } else {
        await api.artists.create(input);
        navigate('/artists');
      }
    } catch (err) {
      setErrors(err instanceof ApiError ? err.errors : ['Failed to save artist.']);
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <p className="text-slate-400">Loading…</p>;

  return (
    <div className="mx-auto max-w-xl">
      <h1 className="mb-6 text-2xl font-semibold text-white">{editing ? 'Edit artist' : 'New artist'}</h1>

      <form onSubmit={submit} className="space-y-4">
        <Field label="Name" error={fieldErrors.name}>
          <input
            className={`${inputClass} ${fieldErrors.name ? inputErrorClass : ''}`}
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Zaie"
            autoFocus
          />
        </Field>

        <Field label="Notes" hint="Optional">
          <textarea className={inputClass} rows={2} value={notes} onChange={(e) => setNotes(e.target.value)} />
        </Field>

        {errors.length > 0 && (
          <ul className="mb-4 rounded-lg bg-red-500/10 px-4 py-2 text-sm text-red-300">
            {errors.map((msg) => (
              <li key={msg}>{msg}</li>
            ))}
          </ul>
        )}

        <div className="flex gap-2">
          <Button type="submit" disabled={saving}>
            {saving ? 'Saving…' : editing ? 'Save' : 'Create artist'}
          </Button>
          <Button type="button" variant="ghost" onClick={goBack}>
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
