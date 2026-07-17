import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { Artist, SongArtistInput } from '@/types';
import { Button, Field, inputClass, inputErrorClass } from '@/components';
import { useBackNavigation } from '@/hooks/useBackNavigation';
import { SongArtistsEditor } from './components/SongArtistsEditor';

/**
 * Create a catalog song directly (2.0 improvement). Follows the same rule as releases — at least one
 * artist must exist first. The song is born an orphan (no release links) until linked from a release.
 */
export default function SongFormPage() {
  const navigate = useNavigate();
  const goBack = useBackNavigation();

  const [artists, setArtists] = useState<Artist[]>([]);
  const [loading, setLoading] = useState(true);
  const [errors, setErrors] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<{ title?: string }>({});

  const [title, setTitle] = useState('');
  const [mainArtistId, setMainArtistId] = useState('');
  const [isrc, setIsrc] = useState('');
  const [songArtists, setSongArtists] = useState<SongArtistInput[]>([]);

  useEffect(() => {
    (async () => {
      try {
        const arts = await api.artists.list();
        setArtists(arts);
        if (arts.length > 0) setMainArtistId(arts[0].id);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  // Switching the main artist drops it from any feat/collab selection (the editor already hides it).
  function changeMainArtist(id: string) {
    setMainArtistId(id);
    setSongArtists((prev) => prev.filter((a) => a.artistId !== id));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErrors([]);
    setFieldErrors({});

    if (!title.trim()) {
      setFieldErrors({ title: 'Song title is required.' });
      return;
    }

    setSaving(true);
    try {
      await api.songs.create({
        title: title.trim(),
        mainArtistId,
        isrc: isrc.trim() || null,
        artists: songArtists,
      });
      goBack();
    } catch (err) {
      setErrors(err instanceof ApiError ? err.errors : ['Failed to save song.']);
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <p className="text-slate-400">Loading…</p>;

  if (artists.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center">
        <p className="text-slate-300">You need at least one artist before adding a song.</p>
        <Button className="mt-4" onClick={() => navigate('/artists')}>
          Go to Artists
        </Button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-xl">
      <h1 className="mb-6 text-2xl font-semibold text-white">New song</h1>

      <form onSubmit={submit} className="space-y-4">
        <Field label="Title" error={fieldErrors.title}>
          <input
            className={`${inputClass} ${fieldErrors.title ? inputErrorClass : ''}`}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="e.g. Luz"
            autoFocus
          />
        </Field>

        <Field label="Main artist">
          <select className={inputClass} value={mainArtistId} onChange={(e) => changeMainArtist(e.target.value)}>
            {artists.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </select>
        </Field>

        <Field label="ISRC" hint="Optional — assigned at distribution">
          <input
            className={`${inputClass} max-w-[16rem]`}
            value={isrc}
            onChange={(e) => setIsrc(e.target.value)}
            placeholder="e.g. USABC1234567"
          />
        </Field>

        <Field label="Featured artists & collaborators" hint="Optional">
          <SongArtistsEditor
            artists={artists}
            value={songArtists}
            onChange={setSongArtists}
            mainArtistId={mainArtistId}
          />
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
            {saving ? 'Saving…' : 'Create song'}
          </Button>
          <Button type="button" variant="ghost" onClick={goBack}>
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
