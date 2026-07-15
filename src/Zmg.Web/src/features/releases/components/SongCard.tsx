import { Link } from 'react-router-dom';
import type { TrackDto } from '@/types';

/**
 * A single's one song, shown on the release detail (M13) in place of the one-row tracklist. Title
 * links into the catalog where the song is edited; main artist + an ISRC indicator are read-only here.
 */
export function SongCard({ track, mainArtistName }: { track: TrackDto; mainArtistName: string }) {
  return (
    <section className="overflow-hidden rounded-xl border border-edge bg-panel">
      <div className="px-4 py-3 font-semibold text-white">Song</div>
      <div className="flex items-center justify-between gap-3 border-t border-edge px-4 py-3">
        <div className="min-w-0">
          <Link to={`/catalog/${track.songId}`} className="text-sm font-medium text-slate-100 hover:text-accent">
            {track.title}
          </Link>
          <p className="text-xs text-slate-400">{mainArtistName}</p>
        </div>
        <span className="shrink-0 text-xs text-slate-500">
          {track.isrc ? `ISRC ${track.isrc}` : 'No ISRC'}
        </span>
      </div>
    </section>
  );
}
