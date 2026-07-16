import { useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import type { Artist, SongArtistInput, SongListItem } from '@/types';
import { InlineAddForm } from '@/components';
import { SongPickerModal } from '@/features/catalog/components/SongPickerModal';
import { NewTrackForm } from './NewTrackForm';

/** A brand-new song to put on the release: a title plus optional ISRC/feats. */
export interface NewTrackDraft {
  title: string;
  isrc: string | null;
  artists: SongArtistInput[];
}

/**
 * One row of a tracklist, in either context (M18). A create-form row has no `songId` until it's
 * saved (and may carry a `details` disclosure for the new song's fields); a detail-page row always
 * has one, so its title links into the catalog.
 */
export interface TracklistRow {
  key: string;
  trackNumber: number;
  title: string;
  isrc: string | null;
  songId: string | null;
  isFocusTrack?: boolean;
  isSongArchived?: boolean;
  details?: ReactNode;
}

/**
 * The album tracklist, shared by the create form and the release detail (M18) — one row design and
 * one set of controls (↑/↓ + ✕) for both, replacing the old split between inline arrows and a kebab
 * menu. It owns no persistence: each context passes callbacks (local rows in the create form, the
 * `api.tracks.*` calls on the detail page) plus the `SongPickerModal`'s artist scope.
 *
 * `onToggleFocus` is optional — the focus track only exists once the release is saved.
 */
export function Tracklist({
  rows,
  readOnly = false,
  mainArtistId,
  excludeIds,
  artists,
  onMove,
  onToggleFocus,
  onRemove,
  onAddNew,
  onAddExisting,
  heading = 'Tracklist',
  emptyText = 'No tracks yet.',
}: {
  rows: TracklistRow[];
  readOnly?: boolean;
  mainArtistId: string;
  excludeIds: string[];
  /** Full artist list — when given, "+ Add track" collects a new song's ISRC/feats at add time. */
  artists?: Artist[];
  onMove: (row: TracklistRow, dir: -1 | 1) => void;
  onToggleFocus?: (row: TracklistRow) => void;
  onRemove: (row: TracklistRow) => void;
  onAddNew: (draft: NewTrackDraft) => void;
  onAddExisting: (song: SongListItem) => void;
  heading?: string;
  emptyText?: string;
}) {
  const [pickerOpen, setPickerOpen] = useState(false);
  const editable = !readOnly;

  return (
    <section className="overflow-hidden rounded-xl border border-edge bg-panel">
      <div className="px-4 py-3 font-semibold text-white">
        {heading} <span className="text-sm font-normal text-slate-400">({rows.length})</span>
      </div>

      <div className="border-t border-edge">
        {rows.length === 0 ? (
          <p className="px-4 py-3 text-sm text-slate-500">{emptyText}</p>
        ) : (
          <ul>
            {rows.map((row, i) => (
              <li key={row.key} className="border-b border-edge/50 last:border-b-0">
                <div className="flex items-center gap-3 px-4 py-2.5">
                  <span className="w-6 shrink-0 text-right text-sm tabular-nums text-slate-500">
                    {row.trackNumber}
                  </span>

                  {onToggleFocus && (
                    <button
                      type="button"
                      aria-label={row.isFocusTrack ? 'Unset focus track' : 'Set focus track'}
                      aria-pressed={row.isFocusTrack}
                      disabled={!editable}
                      onClick={() => onToggleFocus(row)}
                      className={`shrink-0 text-lg leading-none transition ${
                        row.isFocusTrack ? 'text-amber-400' : 'text-slate-600 hover:text-slate-400'
                      } ${editable ? '' : 'cursor-default'}`}
                    >
                      {row.isFocusTrack ? '★' : '☆'}
                    </button>
                  )}

                  <span className="flex-1 text-sm">
                    {row.songId ? (
                      <Link to={`/catalog/${row.songId}`} className="text-slate-100 hover:text-accent">
                        {row.title}
                      </Link>
                    ) : (
                      <span className="text-slate-100">{row.title}</span>
                    )}
                    {row.isFocusTrack && <span className="ml-2 text-xs text-amber-400/80">focus</span>}
                    {row.isSongArchived && <span className="ml-2 text-xs text-slate-500">archived</span>}
                    {row.isrc && <span className="ml-2 text-xs text-slate-500">{row.isrc}</span>}
                  </span>

                  {editable && (
                    <div className="flex shrink-0 items-center gap-1">
                      <button
                        type="button"
                        aria-label="Move up"
                        disabled={i === 0}
                        onClick={() => onMove(row, -1)}
                        className="px-1.5 text-slate-500 hover:text-slate-300 disabled:opacity-30"
                      >
                        ↑
                      </button>
                      <button
                        type="button"
                        aria-label="Move down"
                        disabled={i === rows.length - 1}
                        onClick={() => onMove(row, 1)}
                        className="px-1.5 text-slate-500 hover:text-slate-300 disabled:opacity-30"
                      >
                        ↓
                      </button>
                      <button
                        type="button"
                        aria-label="Remove track"
                        onClick={() => onRemove(row)}
                        className="ml-3 px-1.5 text-red-400/70 hover:text-red-300"
                      >
                        ✕
                      </button>
                    </div>
                  )}
                </div>

                {row.details && <div className="px-4 pb-3 pl-9">{row.details}</div>}
              </li>
            ))}
          </ul>
        )}

        {editable && (
          <>
            {artists ? (
              <NewTrackForm artists={artists} mainArtistId={mainArtistId} onAdd={onAddNew} />
            ) : (
              <InlineAddForm
                addLabel="+ Add track"
                placeholder="New track title"
                onAdd={(title) => onAddNew({ title, isrc: null, artists: [] })}
              />
            )}
            <div className="border-t border-edge px-3 py-2">
              <button
                type="button"
                disabled={!mainArtistId}
                onClick={() => setPickerOpen(true)}
                className="rounded-lg px-2 py-1.5 text-sm text-slate-400 hover:text-accent disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:text-slate-400"
              >
                + Add existing song
              </button>
              {!mainArtistId && (
                <span className="ml-2 text-xs text-slate-500">Pick a main artist first</span>
              )}
            </div>
          </>
        )}
      </div>

      <SongPickerModal
        open={pickerOpen}
        mainArtistId={mainArtistId}
        excludeIds={excludeIds}
        onClose={() => setPickerOpen(false)}
        onSelect={(song) => {
          setPickerOpen(false);
          onAddExisting(song);
        }}
      />
    </section>
  );
}
