import type { TrackDto } from '@/types';
import { InlineAddForm } from '@/components';
import { SongPicker } from '@/features/catalog/components/SongPicker';
import { TrackRow } from './TrackRow';

/**
 * The release's tracklist (v2.0). Renders for both types: an album shows full controls (add a new
 * track or an existing catalog song, reorder, focus, remove); a single shows its one row read-only
 * (its track is fixed at create). Archived releases are read-only too.
 */
export function TrackList({
  tracks,
  isSingle,
  readOnly = false,
  onAdd,
  onAddExisting,
  onToggleFocus,
  onDelete,
  onMove,
}: {
  tracks: TrackDto[];
  isSingle: boolean;
  readOnly?: boolean;
  onAdd: (title: string) => void;
  onAddExisting: (songId: string) => void;
  onToggleFocus: (t: TrackDto) => void;
  onDelete: (t: TrackDto) => void;
  onMove: (t: TrackDto, dir: -1 | 1) => void;
}) {
  // Singles are fixed at one track; only albums (that aren't archived) get add/remove/reorder.
  const showControls = !isSingle && !readOnly;

  return (
    <section className="overflow-hidden rounded-xl border border-edge bg-panel">
      <div className="px-4 py-3 font-semibold text-white">
        {isSingle ? 'Song' : 'Tracklist'}{' '}
        <span className="text-sm font-normal text-slate-400">({tracks.length})</span>
      </div>

      <div className="border-t border-edge">
        {tracks.length === 0 ? (
          <p className="px-4 py-3 text-sm text-slate-500">No tracks yet.</p>
        ) : (
          <ul>
            {tracks.map((t, i) => (
              <TrackRow
                key={t.songId}
                track={t}
                isFirst={i === 0}
                isLast={i === tracks.length - 1}
                showControls={showControls}
                onToggleFocus={onToggleFocus}
                onDelete={onDelete}
                onMove={onMove}
              />
            ))}
          </ul>
        )}

        {showControls && (
          <>
            <InlineAddForm addLabel="+ Add track" placeholder="New track title" onAdd={onAdd} />
            <div className="border-t border-edge px-3 py-2">
              <p className="mb-1 text-xs text-slate-500">…or add an existing song from the catalog</p>
              <SongPicker excludeIds={tracks.map((t) => t.songId)} onSelect={(s) => onAddExisting(s.id)} />
            </div>
          </>
        )}
      </div>
    </section>
  );
}
