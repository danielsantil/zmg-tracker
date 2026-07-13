import type { ArtistRole, ReleaseType } from './enums';
import type { PhaseGroup } from './task';
import type { TrackDto } from './track';

export interface ReleaseArtistInput {
  artistId: string;
  role: ArtistRole;
}

export interface ReleaseInput {
  title: string;
  type: ReleaseType;
  releaseDate: string | null; // yyyy-MM-dd
  mainArtistId: string;
  coverUrl: string | null;
  notes: string | null;
  featuredArtists: ReleaseArtistInput[];
  upc: string | null;
  isrc: string | null;
}

export interface ReleaseListItem {
  id: string;
  title: string;
  type: ReleaseType;
  releaseDate: string;
  mainArtistId: string;
  mainArtistName: string;
  coverUrl: string | null;
  doneTasks: number;
  totalTasks: number;
  status: string;
  upc: string | null;
  isrc: string | null;
  needsIdentifierWarning: boolean;
}

export interface FeaturedArtist {
  artistId: string;
  name: string;
  role: ArtistRole;
}

export interface ReleaseDetail {
  id: string;
  title: string;
  type: ReleaseType;
  releaseDate: string;
  mainArtistId: string;
  mainArtistName: string;
  coverUrl: string | null;
  notes: string | null;
  status: string;
  featuredArtists: FeaturedArtist[];
  doneTasks: number;
  totalTasks: number;
  phases: PhaseGroup[];
  tracks: TrackDto[];
  upc: string | null;
  isrc: string | null;
  needsIdentifierWarning: boolean;
}

export interface CreatedWithWarnings<T> {
  data: T;
  warnings: string[];
}
