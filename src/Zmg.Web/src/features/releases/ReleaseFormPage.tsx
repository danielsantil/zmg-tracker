import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { Artist, ReleaseArtistInput } from '@/types';
import { ArtistRole, ReleaseType } from '@/types';
import { Button, Field, inputClass } from '@/components';
import { useBackNavigation } from '@/hooks/useBackNavigation';

// Known seeded template sizes (build-plan.md §5.4). A template endpoint arrives in M3;
// until then these drive the "checklist will start from N tasks" hint.
const TEMPLATE_TASK_COUNT: Record<ReleaseType, number> = {
  [ReleaseType.Single]: 31,
  [ReleaseType.Album]: 40,
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

  const [title, setTitle] = useState('');
  const [type, setType] = useState<ReleaseType>(ReleaseType.Single);
  const [releaseDate, setReleaseDate] = useState('');
  const [mainArtistId, setMainArtistId] = useState('');
  const [coverUrl, setCoverUrl] = useState('');
  const [notes, setNotes] = useState('');
  const [upc, setUpc] = useState('');
  const [isrc, setIsrc] = useState('');
  const [featured, setFeatured] = useState<ReleaseArtistInput[]>([]);

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
          setIsrc(r.isrc ?? '');
          setFeatured(r.featuredArtists.map((f) => ({ artistId: f.artistId, role: f.role })));
        } else if (arts.length > 0) {
          setMainArtistId(arts[0].id);
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [id, isEdit]);

  function toggleFeatured(artistId: string) {
    setFeatured((prev) =>
      prev.some((f) => f.artistId === artistId)
        ? prev.filter((f) => f.artistId !== artistId)
        : [...prev, { artistId, role: ArtistRole.Featured }],
    );
  }

  function setRole(artistId: string, role: ArtistRole) {
    setFeatured((prev) => prev.map((f) => (f.artistId === artistId ? { ...f, role } : f)));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors([]);
    setWarnings([]);
    try {
      const input = {
        title,
        type,
        releaseDate: releaseDate || null,
        mainArtistId,
        coverUrl: coverUrl || null,
        notes: notes || null,
        upc: upc || null,
        isrc: isrc || null,
        featuredArtists: featured.filter((f) => f.artistId !== mainArtistId),
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

  const otherArtists = artists.filter((a) => a.id !== mainArtistId);

  return (
    <div className="mx-auto max-w-xl">
      <h1 className="mb-6 text-2xl font-semibold text-white">{isEdit ? 'Edit release' : 'New release'}</h1>

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
          <div className="mt-3 flex gap-2">
            <Button variant="ghost" onClick={goBack}>
              Go back
            </Button>
            <Button variant="ghost" onClick={() => setWarnings([])}>
              Keep editing
            </Button>
          </div>
        </div>
      )}

      <form onSubmit={submit} className="space-y-4">
        <Field label="Title">
          <input className={inputClass} value={title} onChange={(e) => setTitle(e.target.value)} placeholder="e.g. Luz" autoFocus />
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
              onChange={(e) => setType(Number(e.target.value) as ReleaseType)}
              disabled={isEdit}
            >
              <option value={ReleaseType.Single}>Single</option>
              <option value={ReleaseType.Album}>Album</option>
            </select>
          </Field>
          <Field label="Release date">
            <input type="date" className={inputClass} value={releaseDate} onChange={(e) => setReleaseDate(e.target.value)} />
          </Field>
        </div>

        {!isEdit && (
          <p className="rounded-lg bg-accent/10 px-3 py-2 text-sm text-accent">
            Checklist will start from the {type === ReleaseType.Album ? 'Album' : 'Single'} template (
            {TEMPLATE_TASK_COUNT[type]} tasks).
          </p>
        )}

        {otherArtists.length > 0 && (
          <Field label="Featured / collab artists" hint="Optional">
            <div className="space-y-2 rounded-lg border border-edge bg-panel p-3">
              {otherArtists.map((a) => {
                const entry = featured.find((f) => f.artistId === a.id);
                return (
                  <div key={a.id} className="flex items-center gap-3">
                    <label className="flex flex-1 items-center gap-2 text-sm text-slate-200">
                      <input type="checkbox" checked={Boolean(entry)} onChange={() => toggleFeatured(a.id)} />
                      {a.name}
                    </label>
                    {entry && (
                      <select
                        className={`${inputClass} max-w-[9rem]`}
                        value={entry.role}
                        onChange={(e) => setRole(a.id, Number(e.target.value) as ArtistRole)}
                      >
                        <option value={ArtistRole.Featured}>Featured</option>
                        <option value={ArtistRole.Collab}>Collab</option>
                      </select>
                    )}
                  </div>
                );
              })}
            </div>
          </Field>
        )}

        <Field label="Cover URL" hint="Optional — shown on release cards">
          <input className={inputClass} value={coverUrl} onChange={(e) => setCoverUrl(e.target.value)} placeholder="https://…" />
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="UPC" hint="Optional — blank until DSP distribution">
            <input className={inputClass} value={upc} onChange={(e) => setUpc(e.target.value)} placeholder="e.g. 0123456789012" />
          </Field>
          <Field label="ISRC" hint="Optional — blank until DSP distribution">
            <input className={inputClass} value={isrc} onChange={(e) => setIsrc(e.target.value)} placeholder="e.g. US-XXX-YY-NNNNN" />
          </Field>
        </div>

        <Field label="Notes" hint="Optional">
          <textarea className={inputClass} rows={3} value={notes} onChange={(e) => setNotes(e.target.value)} />
        </Field>

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
