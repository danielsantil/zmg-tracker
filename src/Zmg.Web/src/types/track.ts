import type { SongArtistDto, SongArtistInput } from './song';

// A track is a Release↔Song join (v2.0), addressed by songId. Title/ISRC/artists come from the song.
export interface TrackDto {
  songId: string;
  trackNumber: number;
  title: string;
  isrc: string | null;
  isFocusTrack: boolean;
  isSongArchived: boolean; // badge on archived-release detail (M15)
  artists: SongArtistDto[];
}

// One row in the create-form Tracks section / add-track payload: exactly one of songId (existing
// catalog song) or title (new inline song). ISRC/artists apply only to a new song.
export interface TrackInput {
  songId: string | null;
  title: string | null;
  isrc: string | null;
  artists: SongArtistInput[] | null;
}

/** Reorder payload: the full ordered song-id list for a release's tracklist. */
export interface TrackReorderInput {
  orderedSongIds: string[];
}
