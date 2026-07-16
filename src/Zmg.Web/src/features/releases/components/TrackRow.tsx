import { Link } from 'react-router-dom';
import type { TrackDto } from '@/types';
import { MenuItem, RowMenu } from '@/components';

/**
 * One track row (v2.0). The song's title/ISRC live on the catalog; this row only reorders, sets the
 * focus track, or removes the link. Renames moved to the catalog (M13). When `showControls` is false
 * (a single, or an archived release) the row is purely informational.
 */
export function TrackRow({
  track,
  isFirst,
  isLast,
  showControls,
  onToggleFocus,
  onDelete,
  onMove,
}: {
  track: TrackDto;
  isFirst: boolean;
  isLast: boolean;
  showControls: boolean;
  onToggleFocus: (t: TrackDto) => void;
  onDelete: (t: TrackDto) => void;
  onMove: (t: TrackDto, dir: -1 | 1) => void;
}) {
  return (
    <li className="border-b border-edge/50 last:border-b-0">
      <div className="flex items-center gap-3 px-4 py-2.5">
        <span className="w-6 shrink-0 text-right text-sm tabular-nums text-slate-500">
          {track.trackNumber}
        </span>

        <button
          aria-label={track.isFocusTrack ? 'Unset focus track' : 'Set focus track'}
          aria-pressed={track.isFocusTrack}
          disabled={!showControls}
          onClick={() => showControls && onToggleFocus(track)}
          className={`shrink-0 text-lg leading-none transition ${
            track.isFocusTrack ? 'text-amber-400' : 'text-slate-600 hover:text-slate-400'
          } ${showControls ? '' : 'cursor-default'}`}
        >
          {track.isFocusTrack ? '★' : '☆'}
        </button>

        <span className="flex-1 text-sm">
          <Link to={`/catalog/${track.songId}`} className="text-slate-100 hover:text-accent">
            {track.title}
          </Link>
          {track.isFocusTrack && <span className="ml-2 text-xs text-amber-400/80">focus</span>}
          {track.isSongArchived && <span className="ml-2 text-xs text-slate-500">archived</span>}
          {track.isrc && <span className="ml-2 text-xs text-slate-500">{track.isrc}</span>}
        </span>

        {showControls && (
          <RowMenu label="Track actions">
            {(close) => (
              <>
                <MenuItem onClick={() => { close(); onToggleFocus(track); }}>
                  {track.isFocusTrack ? 'Unset focus track' : 'Set focus track'}
                </MenuItem>
                {!isFirst && (
                  <MenuItem onClick={() => { close(); onMove(track, -1); }}>Move up</MenuItem>
                )}
                {!isLast && (
                  <MenuItem onClick={() => { close(); onMove(track, 1); }}>Move down</MenuItem>
                )}
                <MenuItem tone="danger" onClick={() => { close(); onDelete(track); }}>
                  Remove from release
                </MenuItem>
              </>
            )}
          </RowMenu>
        )}
      </div>
    </li>
  );
}
