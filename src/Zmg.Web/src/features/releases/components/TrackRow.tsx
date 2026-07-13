import { useState } from 'react';
import type { TrackDto } from '@/types';
import { MenuItem, RowMenu, inputClass } from '@/components';

export function TrackRow({
  track,
  isFirst,
  isLast,
  onRename,
  onToggleFocus,
  onDelete,
  onMove,
}: {
  track: TrackDto;
  isFirst: boolean;
  isLast: boolean;
  onRename: (t: TrackDto, title: string) => void;
  onToggleFocus: (t: TrackDto) => void;
  onDelete: (t: TrackDto) => void;
  onMove: (t: TrackDto, dir: -1 | 1) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState('');

  function startEdit() {
    setDraft(track.title);
    setEditing(true);
  }

  function saveEdit() {
    const title = draft.trim();
    if (title && title !== track.title) onRename(track, title);
    setEditing(false);
  }

  return (
    <li className="border-b border-edge/50 last:border-b-0">
      <div className="flex items-center gap-3 px-4 py-2.5">
        <span className="w-6 shrink-0 text-right text-sm tabular-nums text-slate-500">
          {track.trackNumber}
        </span>

        <button
          aria-label={track.isFocusTrack ? 'Unset focus track' : 'Set focus track'}
          aria-pressed={track.isFocusTrack}
          onClick={() => onToggleFocus(track)}
          className={`shrink-0 text-lg leading-none transition ${
            track.isFocusTrack ? 'text-amber-400' : 'text-slate-600 hover:text-slate-400'
          }`}
        >
          {track.isFocusTrack ? '★' : '☆'}
        </button>

        {editing ? (
          <input
            autoFocus
            className={inputClass}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onBlur={saveEdit}
            onKeyDown={(e) => {
              if (e.key === 'Enter') saveEdit();
              if (e.key === 'Escape') setEditing(false);
            }}
          />
        ) : (
          <button className="flex-1 text-left text-sm text-slate-100" onClick={startEdit}>
            {track.title}
            {track.isFocusTrack && (
              <span className="ml-2 text-xs text-amber-400/80">focus</span>
            )}
          </button>
        )}

        <RowMenu label="Track actions">
          {(close) => (
            <>
              <MenuItem onClick={() => { close(); startEdit(); }}>Rename</MenuItem>
              <MenuItem onClick={() => { close(); onToggleFocus(track); }}>
                {track.isFocusTrack ? 'Unset focus track' : 'Set focus track'}
              </MenuItem>
              {!isFirst && (
                <MenuItem onClick={() => { close(); onMove(track, -1); }}>Move up</MenuItem>
              )}
              {!isLast && (
                <MenuItem onClick={() => { close(); onMove(track, 1); }}>Move down</MenuItem>
              )}
              <MenuItem danger onClick={() => { close(); onDelete(track); }}>
                Delete
              </MenuItem>
            </>
          )}
        </RowMenu>
      </div>
    </li>
  );
}
