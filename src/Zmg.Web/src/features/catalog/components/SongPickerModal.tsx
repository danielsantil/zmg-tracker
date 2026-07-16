import { useEffect, useState } from 'react';
import { api } from '@/api';
import type { SongListItem } from '@/types';
import { Modal, inputClass } from '@/components';

/**
 * Pick an existing catalog song to put on a release (M18). Replaces the inline `SongPicker`: it
 * browses the release's main artist on open — no typing needed when you've forgotten the title —
 * and every query stays scoped to that artist, so another artist's songs can never be linked here.
 * Songs already on the release (`excludeIds`) are filtered out client-side.
 */
export function SongPickerModal({
  open,
  mainArtistId,
  excludeIds,
  onSelect,
  onClose,
}: {
  open: boolean;
  mainArtistId: string;
  excludeIds: string[];
  onSelect: (song: SongListItem) => void;
  onClose: () => void;
}) {
  const [q, setQ] = useState('');
  const [results, setResults] = useState<SongListItem[]>([]);
  const [loading, setLoading] = useState(true);

  // Start each visit from a clean slate rather than the last search.
  useEffect(() => {
    if (!open) {
      setQ('');
      setResults([]);
      setLoading(true);
    }
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const term = q.trim();
    setLoading(true);
    // Browse-on-open is immediate; only typing pays the 250ms debounce.
    const t = setTimeout(async () => {
      try {
        setResults(await api.songs.list({ artistId: mainArtistId, q: term || undefined }));
      } catch {
        setResults([]);
      } finally {
        setLoading(false);
      }
    }, term ? 250 : 0);
    return () => clearTimeout(t);
  }, [open, q, mainArtistId]);

  const visible = results.filter((s) => !excludeIds.includes(s.id));

  return (
    <Modal open={open} onClose={onClose} title="Add existing song">
      <input
        autoFocus
        className={inputClass}
        value={q}
        onChange={(e) => setQ(e.target.value)}
        placeholder="Filter by title…"
      />

      <div className="mt-3 max-h-[50vh] overflow-y-auto rounded-lg border border-edge">
        {loading ? (
          <p className="px-3 py-2 text-sm text-slate-500">Loading…</p>
        ) : visible.length === 0 ? (
          <p className="px-3 py-2 text-sm text-slate-500">
            {q.trim() ? 'No matches.' : 'No songs by this artist yet.'}
          </p>
        ) : (
          <ul>
            {visible.map((s) => (
              <li key={s.id} className="border-b border-edge/50 last:border-b-0">
                <button
                  type="button"
                  onClick={() => onSelect(s)}
                  className="block w-full px-3 py-2 text-left hover:bg-edge/40"
                >
                  <span className="block text-sm text-slate-100">{s.title}</span>
                  <span className="block text-xs text-slate-500">
                    {s.releaseDate ?? 'Unreleased'}
                    {s.isrc && ` · ${s.isrc}`}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </Modal>
  );
}
