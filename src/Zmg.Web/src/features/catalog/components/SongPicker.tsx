import { useEffect, useState } from 'react';
import { api } from '@/api';
import type { SongListItem } from '@/types';
import { inputClass } from '@/components';

/**
 * Shared catalog search+select (M13). Debounced `GET /api/songs?q=`, excluding songs already on the
 * release (`excludeIds`). Wired into the create-form Tracks section and the release-detail album
 * add-row so an existing song can be linked instead of re-entered.
 */
export function SongPicker({
  excludeIds,
  onSelect,
  placeholder = 'Search the catalog…',
  autoFocus = false,
}: {
  excludeIds: string[];
  onSelect: (song: SongListItem) => void;
  placeholder?: string;
  autoFocus?: boolean;
}) {
  const [q, setQ] = useState('');
  const [results, setResults] = useState<SongListItem[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const term = q.trim();
    if (!term) {
      setResults([]);
      return;
    }
    const t = setTimeout(async () => {
      setLoading(true);
      try {
        setResults(await api.songs.list({ q: term }));
      } catch {
        setResults([]);
      } finally {
        setLoading(false);
      }
    }, 250);
    return () => clearTimeout(t);
  }, [q]);

  const visible = results.filter((s) => !excludeIds.includes(s.id));

  return (
    <div>
      <input
        className={inputClass}
        value={q}
        autoFocus={autoFocus}
        onChange={(e) => setQ(e.target.value)}
        placeholder={placeholder}
      />
      {q.trim() && (
        <div className="mt-1 max-h-48 overflow-y-auto rounded-lg border border-edge bg-panel">
          {loading ? (
            <p className="px-3 py-2 text-sm text-slate-500">Searching…</p>
          ) : visible.length === 0 ? (
            <p className="px-3 py-2 text-sm text-slate-500">No matching songs.</p>
          ) : (
            <ul>
              {visible.map((s) => (
                <li key={s.id}>
                  <button
                    type="button"
                    onClick={() => {
                      onSelect(s);
                      setQ('');
                      setResults([]);
                    }}
                    className="flex w-full items-baseline justify-between gap-3 px-3 py-2 text-left text-sm hover:bg-edge/40"
                  >
                    <span className="text-slate-100">{s.title}</span>
                    <span className="shrink-0 text-xs text-slate-400">{s.mainArtistName}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
