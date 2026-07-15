import type { Artist, TrackInput } from '@/types';
import { ArtistRole, ReleaseType } from '@/types';
import { Button, Field, inputClass } from '@/components';

// A create-form row is always a NEW inline song in M12 (the existing-song picker lands in M13),
// so title is edited as a plain string; the payload coerces blank ISRC/empty artists to null.
export function emptyTrack(): TrackInput {
  return { songId: null, title: '', isrc: null, artists: [] };
}

/**
 * The create-form Tracks section (v2.0 M12): rows of inline new tracks (Name, optional ISRC,
 * feats/collabs with role). A single has exactly one fixed row; an album has zero or more with
 * add/remove/reorder. Client-side guards mirror the API 400s.
 */
export function TracksEditor({
  type,
  value,
  onChange,
  artists,
  mainArtistId,
}: {
  type: ReleaseType;
  value: TrackInput[];
  onChange: (tracks: TrackInput[]) => void;
  artists: Artist[];
  mainArtistId: string;
}) {
  const isAlbum = type === ReleaseType.Album;
  const otherArtists = artists.filter((a) => a.id !== mainArtistId);

  function update(i: number, patch: Partial<TrackInput>) {
    onChange(value.map((t, idx) => (idx === i ? { ...t, ...patch } : t)));
  }

  function addRow() {
    onChange([...value, emptyTrack()]);
  }

  function removeRow(i: number) {
    onChange(value.filter((_, idx) => idx !== i));
  }

  function move(i: number, dir: -1 | 1) {
    const j = i + dir;
    if (j < 0 || j >= value.length) return;
    const next = [...value];
    [next[i], next[j]] = [next[j], next[i]];
    onChange(next);
  }

  function toggleArtist(i: number, artistId: string) {
    const current = value[i].artists ?? [];
    const next = current.some((a) => a.artistId === artistId)
      ? current.filter((a) => a.artistId !== artistId)
      : [...current, { artistId, role: ArtistRole.Featured }];
    update(i, { artists: next });
  }

  function setRole(i: number, artistId: string, role: ArtistRole) {
    const next = (value[i].artists ?? []).map((a) => (a.artistId === artistId ? { ...a, role } : a));
    update(i, { artists: next });
  }

  return (
    <Field
      label={isAlbum ? 'Tracks' : 'Track'}
      hint={isAlbum ? 'Add the songs on this album' : 'A single has exactly one song'}
    >
      <div className="space-y-3">
        {value.map((track, i) => (
          <div key={i} className="rounded-lg border border-edge bg-panel p-3">
            <div className="flex items-center gap-2">
              <span className="w-5 shrink-0 text-right text-sm tabular-nums text-slate-500">{i + 1}</span>
              <input
                className={inputClass}
                value={track.title ?? ''}
                onChange={(e) => update(i, { title: e.target.value })}
                placeholder="Song title"
              />
              {isAlbum && (
                <div className="flex shrink-0 items-center gap-1">
                  <button
                    type="button"
                    aria-label="Move up"
                    disabled={i === 0}
                    onClick={() => move(i, -1)}
                    className="px-1.5 text-slate-500 hover:text-slate-300 disabled:opacity-30"
                  >
                    ↑
                  </button>
                  <button
                    type="button"
                    aria-label="Move down"
                    disabled={i === value.length - 1}
                    onClick={() => move(i, 1)}
                    className="px-1.5 text-slate-500 hover:text-slate-300 disabled:opacity-30"
                  >
                    ↓
                  </button>
                  <button
                    type="button"
                    aria-label="Remove track"
                    onClick={() => removeRow(i)}
                    className="px-1.5 text-red-400/70 hover:text-red-300"
                  >
                    ✕
                  </button>
                </div>
              )}
            </div>

            <div className="mt-2 pl-7">
              <input
                className={`${inputClass} max-w-[16rem]`}
                value={track.isrc ?? ''}
                onChange={(e) => update(i, { isrc: e.target.value })}
                placeholder="ISRC (optional)"
              />
            </div>

            {otherArtists.length > 0 && (
              <div className="mt-2 space-y-1.5 pl-7">
                <p className="text-xs text-slate-500">Featured / collab artists</p>
                {otherArtists.map((a) => {
                  const entry = (track.artists ?? []).find((f) => f.artistId === a.id);
                  return (
                    <div key={a.id} className="flex items-center gap-3">
                      <label className="flex flex-1 items-center gap-2 text-sm text-slate-200">
                        <input
                          type="checkbox"
                          checked={Boolean(entry)}
                          onChange={() => toggleArtist(i, a.id)}
                        />
                        {a.name}
                      </label>
                      {entry && (
                        <select
                          className={`${inputClass} max-w-[9rem]`}
                          value={entry.role}
                          onChange={(e) => setRole(i, a.id, Number(e.target.value) as ArtistRole)}
                        >
                          <option value={ArtistRole.Featured}>Featured</option>
                          <option value={ArtistRole.Collab}>Collab</option>
                        </select>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        ))}

        {value.length === 0 && (
          <p className="rounded-lg border border-dashed border-edge px-3 py-2 text-sm text-slate-500">
            No tracks yet — an album can be created empty and filled in later.
          </p>
        )}

        {isAlbum && (
          <Button type="button" variant="ghost" onClick={addRow}>
            + Add track
          </Button>
        )}
      </div>
    </Field>
  );
}
