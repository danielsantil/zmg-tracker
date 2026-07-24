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

/** Query filters for the songs list endpoint. */
export interface SongListFilters {
  q?: string;
  scope?: 'all' | 'archived';
  artistId?: string;
}

// Catalog (M13). ReleaseDate is the one source of truth (M38): the Released column and the Archive
// action both derive from it — null (orphan/archived-only) ⟺ archivable.
export interface SongListItem {
  id: string;
  title: string;
  mainArtistId: string;
  mainArtistName: string;
  releaseDate: string | null; // earliest non-archived linked release date, null for orphans/archived-only
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

// Create a catalog song directly (no release). Same shape as update.
export interface SongCreateInput {
  title: string;
  mainArtistId: string;
  isrc: string | null;
  artists: SongArtistInput[];
}
