import { useState } from 'react';
import type { Artist, SongArtistInput, TrackInput } from '@/types';
import { ReleaseType } from '@/types';
import { Button, Field, inputClass } from '@/components';
import { SongArtistsEditor } from '@/features/catalog/components/SongArtistsEditor';
import { SongPicker } from '@/features/catalog/components/SongPicker';

// A create-form row is either a NEW inline song (title/ISRC/feats editable) or an existing catalog
// song (songId + display title). `key` keeps React state stable across reorder/remove.
interface EditorRow {
  key: string;
  songId: string | null;
  title: string;
  isrc: string;
  artists: SongArtistInput[];
}

let rowSeq = 0;
const newKey = () => `row-${rowSeq++}`;

function blankRow(): EditorRow {
  return { key: newKey(), songId: null, title: '', isrc: '', artists: [] };
}

// Seed one empty new-track row (used by the single's fixed row and the parent's initial state).
export function emptyTrack(): TrackInput {
  return { songId: null, title: '', isrc: null, artists: [] };
}

function toInput(r: EditorRow): TrackInput {
  return r.songId
    ? { songId: r.songId, title: null, isrc: null, artists: null }
    : { songId: null, title: r.title, isrc: r.isrc.trim() || null, artists: r.artists.length ? r.artists : null };
}

/**
 * The create-form Tracks section (v2.0). Each row is a new inline track or an existing catalog song
 * (per-row toggle, M13). A single has exactly one fixed row; an album has zero or more with
 * add/remove/reorder. Emits `TrackInput[]` to the parent on every change. Remount (via `key={type}`
 * in the parent) resets rows when the release type changes.
 */
export function TracksEditor({
  type,
  onChange,
  artists,
  mainArtistId,
}: {
  type: ReleaseType;
  onChange: (tracks: TrackInput[]) => void;
  artists: Artist[];
  mainArtistId: string;
}) {
  const isAlbum = type === ReleaseType.Album;
  const [rows, setRows] = useState<EditorRow[]>(() => (isAlbum ? [] : [blankRow()]));

  function commit(next: EditorRow[]) {
    setRows(next);
    onChange(next.map(toInput));
  }

  function update(i: number, patch: Partial<EditorRow>) {
    commit(rows.map((r, idx) => (idx === i ? { ...r, ...patch } : r)));
  }

  function addRow() {
    commit([...rows, blankRow()]);
  }

  function removeRow(i: number) {
    commit(rows.filter((_, idx) => idx !== i));
  }

  function move(i: number, dir: -1 | 1) {
    const j = i + dir;
    if (j < 0 || j >= rows.length) return;
    const next = [...rows];
    [next[i], next[j]] = [next[j], next[i]];
    commit(next);
  }

  // songIds already chosen in other rows — exclude them from the picker.
  const chosenIds = rows.filter((r) => r.songId).map((r) => r.songId!);

  return (
    <Field
      label={isAlbum ? 'Tracks' : 'Track'}
      hint={isAlbum ? 'Add the songs on this album' : 'A single has exactly one song'}
    >
      <div className="space-y-3">
        {rows.map((track, i) => (
          <div key={track.key} className="rounded-lg border border-edge bg-panel p-3">
            <div className="flex items-center gap-2">
              <span className="w-5 shrink-0 text-right text-sm tabular-nums text-slate-500">{i + 1}</span>

              {track.songId ? (
                // Existing catalog song: title is read-only, "Change" re-opens the picker.
                <div className="flex flex-1 items-center gap-2">
                  <span className="flex-1 text-sm text-slate-100">{track.title}</span>
                  <span className="text-xs text-slate-500">from catalog</span>
                  <button
                    type="button"
                    className="text-xs text-slate-400 hover:text-accent"
                    onClick={() => update(i, { songId: null, title: '' })}
                  >
                    Change
                  </button>
                </div>
              ) : (
                <input
                  className={inputClass}
                  value={track.title}
                  onChange={(e) => update(i, { title: e.target.value })}
                  placeholder="Song title"
                />
              )}

              {isAlbum && (
                <div className="flex shrink-0 items-center gap-1">
                  <button type="button" aria-label="Move up" disabled={i === 0} onClick={() => move(i, -1)}
                    className="px-1.5 text-slate-500 hover:text-slate-300 disabled:opacity-30">↑</button>
                  <button type="button" aria-label="Move down" disabled={i === rows.length - 1} onClick={() => move(i, 1)}
                    className="px-1.5 text-slate-500 hover:text-slate-300 disabled:opacity-30">↓</button>
                  <button type="button" aria-label="Remove track" onClick={() => removeRow(i)}
                    className="px-1.5 text-red-400/70 hover:text-red-300">✕</button>
                </div>
              )}
            </div>

            {/* New-track fields (title / ISRC / feats). Hidden once a catalog song is picked. */}
            {!track.songId && (
              <div className="mt-2 space-y-2 pl-7">
                <input
                  className={`${inputClass} max-w-[16rem]`}
                  value={track.isrc}
                  onChange={(e) => update(i, { isrc: e.target.value })}
                  placeholder="ISRC (optional)"
                />
                <SongArtistsEditor
                  artists={artists}
                  value={track.artists}
                  onChange={(next) => update(i, { artists: next })}
                  mainArtistId={mainArtistId}
                />
                <div>
                  <span className="mr-2 text-xs text-slate-500">or</span>
                  <SongPicker
                    excludeIds={chosenIds}
                    placeholder="Pick an existing song from the catalog…"
                    onSelect={(s) => update(i, { songId: s.id, title: s.title })}
                  />
                </div>
              </div>
            )}
          </div>
        ))}

        {rows.length === 0 && (
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
