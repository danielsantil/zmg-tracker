import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { Artist, TrackInput } from '@/types';
import { ReleaseType } from '@/types';
import { Button, Field, inputClass, inputErrorClass } from '@/components';
import { useBackNavigation } from '@/hooks/useBackNavigation';
import { TracksEditor } from './components/TracksEditor';
import { emptyTrack } from './components/trackInput';

// Known seeded template sizes (build-plan.md §5.4). A template endpoint arrives in M3;
// until then these drive the "checklist will start from N tasks" hint.
const TEMPLATE_TASK_COUNT: Record<ReleaseType, number> = {
  [ReleaseType.Single]: 31,
  [ReleaseType.Album]: 41,
};

export default function ReleaseFormPage() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();
  const goBack = useBackNavigation();

  const [artists, setArtists] = useState<Artist[]>([]);
  const [loading, setLoading] = useState(true);
  const [errors, setErrors] = useState<string[]>([]);
  const [warnings, setWarnings] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  // Per-field client-side validation (title / release date required). Populated on save.
  const [fieldErrors, setFieldErrors] = useState<{ title?: string; releaseDate?: string }>({});

  const [title, setTitle] = useState('');
  const [type, setType] = useState<ReleaseType>(ReleaseType.Single);
  const [releaseDate, setReleaseDate] = useState('');
  const [mainArtistId, setMainArtistId] = useState('');
  const [coverUrl, setCoverUrl] = useState('');
  const [notes, setNotes] = useState('');
  const [upc, setUpc] = useState('');
  // Tracks are create-only. A single starts with exactly one fixed row; an album starts empty.
  const [tracks, setTracks] = useState<TrackInput[]>([emptyTrack()]);

  useEffect(() => {
    (async () => {
      try {
        const arts = await api.artists.list();
        setArtists(arts);
        if (isEdit && id) {
          const r = await api.releases.get(id);
          setTitle(r.title);
          setType(r.type);
          setReleaseDate(r.releaseDate);
          setMainArtistId(r.mainArtistId);
          setCoverUrl(r.coverUrl ?? '');
          setNotes(r.notes ?? '');
          setUpc(r.upc ?? '');
        } else if (arts.length > 0) {
          setMainArtistId(arts[0].id);
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [id, isEdit]);

  // Switching type resets the Tracks section to its type's baseline (single = 1 fixed row, album = empty).
  function changeType(next: ReleaseType) {
    setType(next);
    setTracks(next === ReleaseType.Single ? [emptyTrack()] : []);
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErrors([]);
    setWarnings([]);
    setFieldErrors({});

    // Required-field validation on the FE so missing title/date surface as red fields
    // (not just an API error at the bottom).
    const fe: { title?: string; releaseDate?: string } = {};
    if (!title.trim()) fe.title = 'Release title is required.';
    if (!releaseDate) fe.releaseDate = 'Release date is required.';
    if (fe.title || fe.releaseDate) {
      setFieldErrors(fe);
      return;
    }

    // Client-side guards mirror the API 400s (create only). A row is valid if it's an existing
    // catalog song (songId) or a new title.
    if (!isEdit) {
      const filled = tracks.filter((t) => t.songId || (t.title ?? '').trim());
      if (type === ReleaseType.Single && filled.length !== 1) {
        setErrors(['A single must have exactly one track.']);
        return;
      }
      if (tracks.some((t) => !t.songId && !(t.title ?? '').trim())) {
        setErrors(['Every new track needs a title (or pick an existing song).']);
        return;
      }
    }

    setSaving(true);
    try {
      const cleanedTracks: TrackInput[] = tracks.map((t) =>
        t.songId
          ? { songId: t.songId, title: null, isrc: null, artists: null }
          : {
            songId: null,
            title: (t.title ?? '').trim(),
            isrc: (t.isrc ?? '').trim() || null,
            artists: t.artists && t.artists.length > 0 ? t.artists : null,
          },
      );

      const input = {
        title,
        type,
        releaseDate: releaseDate || null,
        mainArtistId,
        coverUrl: coverUrl || null,
        notes: notes || null,
        upc: upc || null,
        tracks: isEdit ? null : cleanedTracks,
      };
      const result = isEdit && id ? await api.releases.update(id, input) : await api.releases.create(input);
      if (result.warnings.length > 0) {
        setWarnings(result.warnings);
      } else {
        goBack();
      }
    } catch (err) {
      setErrors(err instanceof ApiError ? err.errors : ['Failed to save release.']);
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <p className="text-slate-400">Loading…</p>;

  if (artists.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-edge bg-panel/50 p-10 text-center">
        <p className="text-slate-300">You need at least one artist before creating a release.</p>
        <Button className="mt-4" onClick={() => navigate('/artists')}>
          Go to Artists
        </Button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-xl">
      <h1 className="mb-6 text-2xl font-semibold text-white">{isEdit ? 'Edit release' : 'New release'}</h1>

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
          <select className={inputClass} value={mainArtistId} onChange={(e) => setMainArtistId(e.target.value)}>
            {artists.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </select>
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Release type">
            <select
              className={inputClass}
              value={type}
              onChange={(e) => changeType(Number(e.target.value) as ReleaseType)}
              disabled={isEdit}
            >
              <option value={ReleaseType.Single}>Single</option>
              <option value={ReleaseType.Album}>Album</option>
            </select>
          </Field>
          <Field label="Release date" error={fieldErrors.releaseDate}>
            <input
              type="date"
              className={`${inputClass} ${fieldErrors.releaseDate ? inputErrorClass : ''}`}
              value={releaseDate}
              onChange={(e) => setReleaseDate(e.target.value)}
            />
          </Field>
        </div>

        {!isEdit && (
          <p className="rounded-lg bg-accent/10 px-3 py-2 text-sm text-accent">
            Checklist will start from the {type === ReleaseType.Album ? 'Album' : 'Single'} template (
            {TEMPLATE_TASK_COUNT[type]} tasks).
          </p>
        )}

        <Field label="Cover URL" hint="Optional — shown on release cards">
          <input className={inputClass} value={coverUrl} onChange={(e) => setCoverUrl(e.target.value)} placeholder="https://…" />
        </Field>

        <Field label="UPC" hint="Optional — blank until DSP distribution">
          <input className={`${inputClass} max-w-[16rem]`} value={upc} onChange={(e) => setUpc(e.target.value)} placeholder="e.g. 0123456789012" />
        </Field>

        <Field label="Notes" hint="Optional">
          <textarea className={inputClass} rows={3} value={notes} onChange={(e) => setNotes(e.target.value)} />
        </Field>

        {/* Tracks are set at create only; editing them happens via the release detail (and the catalog). */}
        {!isEdit && (
          <TracksEditor
            key={type}
            type={type}
            onChange={setTracks}
            artists={artists}
            mainArtistId={mainArtistId}
          />
        )}

        {errors.length > 0 && (
          <ul className="mb-4 rounded-lg bg-red-500/10 px-4 py-2 text-sm text-red-300">
            {errors.map((msg) => (
              <li key={msg}>{msg}</li>
            ))}
          </ul>
        )}

        {warnings.length > 0 && (
          <div className="mb-4 rounded-lg bg-amber-500/10 px-4 py-3 text-sm text-amber-200">
            <p className="font-medium">Saved with warnings:</p>
            <ul className="ml-4 list-disc">
              {warnings.map((msg) => (
                <li key={msg}>{msg}</li>
              ))}
            </ul>
            {/* Both live inside the <form>, so they need an explicit type — the HTML default
                is `submit`, which re-fired the create POST and duplicated the release. */}
            <div className="mt-3 flex gap-2">
              <Button type="button" variant="ghost" onClick={goBack}>
                Go back
              </Button>
              <Button type="button" variant="ghost" onClick={() => setWarnings([])}>
                Keep editing
              </Button>
            </div>
          </div>
        )}

        <div className="flex gap-2">
          <Button type="submit" disabled={saving}>
            {saving ? 'Saving…' : isEdit ? 'Save changes' : 'Create release'}
          </Button>
          <Button type="button" variant="ghost" onClick={goBack}>
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
