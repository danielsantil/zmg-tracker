import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, ApiError } from '@/api';
import type { Artist, SongArtistInput, SongDetail } from '@/types';
import { Button, Field, Toast, TypeBadge, inputClass } from '@/components';
import { useToast } from '@/hooks/useToast';
import { useBackNavigation } from '@/hooks/useBackNavigation';
import { SongArtistsEditor } from './components/SongArtistsEditor';

/**
 * Catalog song detail (M13): editable Name / Main artist / ISRC / feats-collabs, plus a read-only
 * list of every linked release (the song's UPCs derive from these). An informational artist-drift
 * hint appears when the song's main artist differs from a linked release's — divergence is
 * intentional (compilations, collab albums), so it never blocks. When the song is archived (M15) all
 * fields are disabled and a read-only note replaces Save; the release links stay clickable.
 */
export default function SongDetailPage() {
  const { id } = useParams<{ id: string }>();
  const goBack = useBackNavigation();
  const { toast, showToast } = useToast();

  const [song, setSong] = useState<SongDetail | null>(null);
  const [artists, setArtists] = useState<Artist[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [warnings, setWarnings] = useState<string[]>([]);

  const [title, setTitle] = useState('');
  const [mainArtistId, setMainArtistId] = useState('');
  const [isrc, setIsrc] = useState('');
  const [songArtists, setSongArtists] = useState<SongArtistInput[]>([]);

  const hydrate = useCallback((s: SongDetail) => {
    setSong(s);
    setTitle(s.title);
    setMainArtistId(s.mainArtistId);
    setIsrc(s.isrc ?? '');
    setSongArtists(s.artists.map((a) => ({ artistId: a.artistId, role: a.role })));
  }, []);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    setError(null);
    Promise.all([api.songs.get(id), api.artists.list()])
      .then(([s, arts]) => {
        hydrate(s);
        setArtists(arts);
      })
      .catch((e) => setError(e instanceof ApiError ? e.message : 'Failed to load song.'))
      .finally(() => setLoading(false));
  }, [id, hydrate]);

  async function save() {
    if (!id) return;
    setSaving(true);
    setErrors([]);
    setWarnings([]);
    try {
      const result = await api.songs.update(id, {
        title,
        mainArtistId,
        isrc: isrc.trim() || null,
        artists: songArtists,
      });
      hydrate(result.data);
      if (result.warnings.length > 0) setWarnings(result.warnings);
      else showToast('Saved.');
    } catch (e) {
      setErrors(e instanceof ApiError ? e.errors : ['Failed to save song.']);
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <p className="text-slate-400">Loading…</p>;
  if (error) return <p className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-300">{error}</p>;
  if (!song) return null;

  // Drift is intentional (compilations, collab albums) — informational only, never blocks.
  const drifts = song.releases.filter((r) => r.mainArtistId !== mainArtistId);
  const archived = song.isArchived;

  return (
    <div className="mx-auto max-w-xl">
      <button onClick={goBack} className="mb-4 text-sm text-slate-400 hover:text-slate-200">
        ‹ Back
      </button>

      <h1 className="mb-6 text-2xl font-semibold text-white">Song</h1>

      {archived && (
        <div className="mb-4 rounded-lg border border-edge bg-panel/50 px-4 py-2.5 text-sm text-slate-300">
          Archived — read only. This song can't be edited or restored.
        </div>
      )}

      <div className="space-y-4">
        <Field label="Name">
          <input className={inputClass} value={title} disabled={archived} onChange={(e) => setTitle(e.target.value)} />
        </Field>

        <Field label="Main artist">
          <select className={inputClass} value={mainArtistId} disabled={archived} onChange={(e) => setMainArtistId(e.target.value)}>
            {artists.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </select>
        </Field>

        {drifts.length > 0 && (
          <div className="rounded-lg border border-edge bg-panel/50 px-3 py-2 text-xs text-slate-400">
            {drifts.map((r) => (
              <p key={r.releaseId}>
                Main artist differs from release <span className="text-slate-300">{r.title}</span> ({r.mainArtistName}).
              </p>
            ))}
          </div>
        )}

        <Field label="ISRC" hint="Optional — blank until DSP distribution">
          <input
            className={`${inputClass} max-w-[16rem]`}
            value={isrc}
            disabled={archived}
            onChange={(e) => setIsrc(e.target.value)}
            placeholder="e.g. US-XXX-YY-NNNNN"
          />
        </Field>

        <Field label="Featured / collab artists" hint="Optional">
          <SongArtistsEditor
            artists={artists}
            value={songArtists}
            onChange={setSongArtists}
            mainArtistId={mainArtistId}
            disabled={archived}
          />
        </Field>

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
        </div>
      )}
      
        {!archived && (
          <div className="flex gap-2">
            <Button onClick={save} disabled={saving}>
              {saving ? 'Saving…' : 'Save changes'}
            </Button>
          </div>
        )}
      </div>

      {/* Read-only: every release this song is on. The song's UPCs derive from here. */}
      <section className="mt-8 overflow-hidden rounded-xl border border-edge bg-panel">
        <div className="px-4 py-3 font-semibold text-white">
          Releases <span className="text-sm font-normal text-slate-400">({song.releases.length})</span>
        </div>
        <div className="border-t border-edge">
          {song.releases.length === 0 ? (
            <p className="px-4 py-3 text-sm text-slate-500">Not on any release yet.</p>
          ) : (
            <ul>
              {song.releases.map((r) => (
                <li key={r.releaseId} className="border-b border-edge/50 last:border-b-0">
                  <Link
                    to={`/releases/${r.releaseId}`}
                    className="flex items-center justify-between gap-3 px-4 py-2.5 hover:bg-edge/40"
                  >
                    <span className="flex min-w-0 items-center gap-2">
                      <span className="truncate text-sm text-slate-100">{r.title}</span>
                      <TypeBadge type={r.type} />
                      {r.isArchived && <span className="text-xs text-slate-500">archived</span>}
                    </span>
                    <span className="shrink-0 text-xs text-slate-400">
                      {r.releaseDate}
                      {r.upc && <span className="ml-2"><b>UPC: {r.upc}</b></span>}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      <Toast message={toast} />
    </div>
  );
}
