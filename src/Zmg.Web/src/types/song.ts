import type { ArtistRole } from './enums';

// Feat/collab artist on a song (v2.0). Full Song types (list/detail) land with the Catalog in M13.
export interface SongArtistInput {
  artistId: string;
  role: ArtistRole;
}

export interface SongArtistDto {
  artistId: string;
  name: string;
  role: ArtistRole;
}
