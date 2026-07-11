// Enums serialized as integers by the API (System.Text.Json default).
export enum ReleaseType {
  Single = 0,
  Album = 1,
}

export enum Phase {
  Pre = 0,
  Release = 1,
  Post = 2,
}

export enum ArtistRole {
  Featured = 0,
  Collab = 1,
}

export interface Artist {
  id: string;
  name: string;
  notes: string | null;
  releaseCount: number;
}

export interface ArtistInput {
  name: string;
  notes: string | null;
}

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
}

export interface FeaturedArtist {
  artistId: string;
  name: string;
  role: ArtistRole;
}

export interface ReleaseTaskDto {
  id: string;
  title: string;
  phase: Phase;
  sortOrder: number;
  isDone: boolean;
  completedAt: string | null;
  notes: string | null;
}

export interface PhaseGroup {
  phase: Phase;
  done: number;
  total: number;
  tasks: ReleaseTaskDto[];
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
}

export interface CreatedWithWarnings<T> {
  data: T;
  warnings: string[];
}
