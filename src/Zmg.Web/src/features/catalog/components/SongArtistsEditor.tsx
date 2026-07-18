import type { Artist, SongArtistInput } from '@/types';
import { ArtistRole } from '@/types';
import { inputClass } from '@/components';

/**
 * Feats/collabs editor for a song (M13) — extracted from the old release-form featured block so the
 * create-form Tracks section and the catalog detail page share one control. A checkbox per non-main
 * artist, each with a Featured/Collab role select.
 */
export function SongArtistsEditor({
  artists,
  value,
  onChange,
  mainArtistId,
  disabled = false,
}: {
  artists: Artist[];
  value: SongArtistInput[];
  onChange: (next: SongArtistInput[]) => void;
  mainArtistId: string;
  disabled?: boolean;
}) {
  const otherArtists = artists.filter((a) => a.id !== mainArtistId);
  if (otherArtists.length === 0) return null;

  function toggle(artistId: string) {
    onChange(
      value.some((a) => a.artistId === artistId)
        ? value.filter((a) => a.artistId !== artistId)
        : [...value, { artistId, role: ArtistRole.Featured }],
    );
  }

  function setRole(artistId: string, role: ArtistRole) {
    onChange(value.map((a) => (a.artistId === artistId ? { ...a, role } : a)));
  }

  return (
    <div className="space-y-1.5">
      {otherArtists.map((a) => {
        const entry = value.find((f) => f.artistId === a.id);
        return (
          <div key={a.id} className="flex items-center gap-3">
            <label className="flex flex-1 items-center gap-2 text-sm text-body">
              <input
                type="checkbox"
                checked={Boolean(entry)}
                disabled={disabled}
                onChange={() => toggle(a.id)}
              />
              {a.name}
            </label>
            {entry && (
              <select
                className={`${inputClass} max-w-[9rem]`}
                value={entry.role}
                disabled={disabled}
                onChange={(e) => setRole(a.id, Number(e.target.value) as ArtistRole)}
              >
                <option value={ArtistRole.Featured}>Featured</option>
                <option value={ArtistRole.Collab}>Collab</option>
              </select>
            )}
          </div>
        );
      })}
    </div>
  );
}
