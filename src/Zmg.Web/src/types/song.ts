import type { ArtistRole, ReleaseType } from './enums';

// Feat/collab artist on a song (v2.0).
export interface SongArtistInput {
  artistId: string;
  role: ArtistRole;
}

export interface SongArtistDto {
  artistId: string;
  name: string;
  role: ArtistRole;
}

// Catalog (M13).
export interface SongListItem {
  id: string;
  title: string;
  mainArtistId: string;
  mainArtistName: string;
  releaseDate: string | null; // earliest non-archived linked release date, null for orphans
  isrc: string | null;
  releaseCount: number;
  isArchived: boolean;
}

export interface SongReleaseLink {
  releaseId: string;
  title: string;
  type: ReleaseType;
  releaseDate: string;
  upc: string | null;
  mainArtistId: string;
  mainArtistName: string;
  isArchived: boolean;
}

export interface SongDetail {
  id: string;
  title: string;
  mainArtistId: string;
  mainArtistName: string;
  isrc: string | null;
  isArchived: boolean;
  artists: SongArtistDto[];
  releases: SongReleaseLink[];
}

export interface SongUpdateInput {
  title: string;
  mainArtistId: string;
  isrc: string | null;
  artists: SongArtistInput[];
}
