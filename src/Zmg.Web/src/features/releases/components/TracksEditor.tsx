import { useState } from 'react';
import type { Artist, SongArtistInput, TrackInput } from '@/types';
import { ReleaseType } from '@/types';
import { Button, Field, inputClass } from '@/components';
import { SongArtistsEditor } from '@/features/catalog/components/SongArtistsEditor';
import { SongPickerModal } from '@/features/catalog/components/SongPickerModal';
import { Tracklist, type TracklistRow } from './Tracklist';

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

function toInput(r: EditorRow): TrackInput {
  return r.songId
    ? { songId: r.songId, title: null, isrc: null, artists: null }
    : { songId: null, title: r.title, isrc: r.isrc.trim() || null, artists: r.artists.length ? r.artists : null };
}

/**
 * The create-form Tracks section (v2.0). Each row is a new inline track or an existing catalog song.
 * An album is the create-form adapter onto the shared `Tracklist` (M18) — same rows and ↑/↓ controls
 * as the release detail, with the new song's optional fields tucked into a per-row disclosure; a
 * single is one fixed row instead, since there's no list to reorder. Emits `TrackInput[]` to the
 * parent on every change. Remount (via `key={type}` in the parent) resets rows when the type changes.
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
  const [singlePickerOpen, setSinglePickerOpen] = useState(false);

  function commit(next: EditorRow[]) {
    setRows(next);
    onChange(next.map(toInput));
  }

  function update(key: string, patch: Partial<EditorRow>) {
    commit(rows.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  }

  function move(key: string, dir: -1 | 1) {
    const i = rows.findIndex((r) => r.key === key);
    const j = i + dir;
    if (j < 0 || j >= rows.length) return;
    const next = [...rows];
    [next[i], next[j]] = [next[j], next[i]];
    commit(next);
  }

  // songIds already chosen in other rows — exclude them from the picker.
  const chosenIds = rows.flatMap((r) => (r.songId ? [r.songId] : []));

  // The new song's own fields. Only for a row that isn't a catalog song (which owns its own).
  function newSongFields(row: EditorRow) {
    return (
      <div className="space-y-2">
        <input
          className={inputClass}
          value={row.title}
          onChange={(e) => update(row.key, { title: e.target.value })}
          placeholder="Song title"
        />
        <input
          className={`${inputClass} max-w-[16rem]`}
          value={row.isrc}
          onChange={(e) => update(row.key, { isrc: e.target.value })}
          placeholder="ISRC (optional)"
        />
        <SongArtistsEditor
          artists={artists}
          value={row.artists}
          onChange={(next) => update(row.key, { artists: next })}
          mainArtistId={mainArtistId}
        />
      </div>
    );
  }

  if (!isAlbum) {
    const row = rows[0];
    return (
      <Field label="Track" hint="A single has exactly one song">
        <div className="rounded-lg border border-edge bg-panel p-3">
          {row.songId ? (
            <div className="flex items-center gap-2">
              <span className="flex-1 text-sm text-strong">{row.title}</span>
              <span className="text-xs text-subtle">from catalog</span>
              <button
                type="button"
                className="text-xs text-muted hover:text-accent"
                onClick={() => update(row.key, { songId: null, title: '' })}
              >
                Change
              </button>
            </div>
          ) : (
            <>
              {newSongFields(row)}
              <div className="mt-2">
                <span className="mr-2 text-xs text-subtle">or</span>
                <Button
                  type="button"
                  variant="ghost"
                  disabled={!mainArtistId}
                  onClick={() => setSinglePickerOpen(true)}
                >
                  Add existing song
                </Button>
                {!mainArtistId && (
                  <span className="ml-2 text-xs text-subtle">Pick a main artist first</span>
                )}
              </div>
            </>
          )}
        </div>

        <SongPickerModal
          open={singlePickerOpen}
          mainArtistId={mainArtistId}
          excludeIds={[]}
          onClose={() => setSinglePickerOpen(false)}
          onSelect={(s) => {
            setSinglePickerOpen(false);
            update(row.key, { songId: s.id, title: s.title });
          }}
        />
      </Field>
    );
  }

  const tracklistRows: TracklistRow[] = rows.map((r, i) => ({
    key: r.key,
    trackNumber: i + 1,
    title: r.title || 'Untitled song',
    // A new song's ISRC is edited in its disclosure below, so don't echo it on the row too.
    isrc: null,
    songId: r.songId,
    details: r.songId ? undefined : (
      <details className="text-sm">
        <summary className="cursor-pointer text-xs text-subtle hover:text-body">
          Details (optional)
        </summary>
        <div className="mt-2">{newSongFields(r)}</div>
      </details>
    ),
  }));

  return (
    <Field label="Tracks" hint="Add the songs on this album">
      <Tracklist
        rows={tracklistRows}
        mainArtistId={mainArtistId}
        excludeIds={chosenIds}
        emptyText="No tracks yet — an album can be created empty and filled in later."
        onMove={(row, dir) => move(row.key, dir)}
        onRemove={(row) => commit(rows.filter((r) => r.key !== row.key))}
        onAddNew={(draft) => commit([...rows, { ...blankRow(), title: draft.title }])}
        onAddExisting={(song) => commit([...rows, { ...blankRow(), songId: song.id, title: song.title }])}
      />
    </Field>
  );
}
