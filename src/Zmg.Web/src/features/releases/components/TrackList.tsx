import type { TrackDto } from '@/types';
import { InlineAddForm } from '@/components';
import { TrackRow } from './TrackRow';

export function TrackList({
  tracks,
  readOnly = false,
  onAdd,
  onRename,
  onToggleFocus,
  onDelete,
  onMove,
}: {
  tracks: TrackDto[];
  readOnly?: boolean;
  onAdd: (title: string) => void;
  onRename: (t: TrackDto, title: string) => void;
  onToggleFocus: (t: TrackDto) => void;
  onDelete: (t: TrackDto) => void;
  onMove: (t: TrackDto, dir: -1 | 1) => void;
}) {
  return (
    <section className="overflow-hidden rounded-xl border border-edge bg-panel">
      <div className="px-4 py-3 font-semibold text-white">
        Tracklist <span className="text-sm font-normal text-slate-400">({tracks.length})</span>
      </div>

      <div className="border-t border-edge">
        {tracks.length === 0 ? (
          <p className="px-4 py-3 text-sm text-slate-500">No tracks yet.</p>
        ) : (
          <ul>
            {tracks.map((t, i) => (
              <TrackRow
                key={t.id}
                track={t}
                isFirst={i === 0}
                isLast={i === tracks.length - 1}
                readOnly={readOnly}
                onRename={onRename}
                onToggleFocus={onToggleFocus}
                onDelete={onDelete}
                onMove={onMove}
              />
            ))}
          </ul>
        )}

        {!readOnly && <InlineAddForm addLabel="+ Add track" placeholder="New track title" onAdd={onAdd} />}
      </div>
    </section>
  );
}
