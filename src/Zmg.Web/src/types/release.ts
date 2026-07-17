import type { ReleaseType } from './enums';
import type { PhaseGroup } from './task';
import type { TrackDto, TrackInput } from './track';

/** The four derived statuses (`ReleaseStatus.cs`). Derived server-side, never stored. */
export type ReleaseStatus = 'Upcoming' | 'Released' | 'Complete' | 'Archived';

/** Query filters for the releases list endpoint. */
export interface ReleaseListFilters {
  artistId?: string;
  type?: ReleaseType;
  status?: string;
  scope?: 'home' | 'all' | 'archived';
  q?: string;
}

export interface ReleaseInput {
  title: string;
  type: ReleaseType;
  releaseDate: string | null; // yyyy-MM-dd
  mainArtistId: string;
  coverUrl: string | null;
  notes: string | null;
  tracks: TrackInput[] | null; // create-only; ignored on update
  upc: string | null;
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
  status: ReleaseStatus;
  upc: string | null;
  warnings: string[]; // soft advisories (e.g. "Missing UPC", "Album is empty")
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
  status: ReleaseStatus;
  doneTasks: number;
  totalTasks: number;
  phases: PhaseGroup[];
  tracks: TrackDto[];
  upc: string | null;
  warnings: string[]; // soft advisories (e.g. "Missing UPC", "Album is empty")
  isArchived: boolean;
}

export interface CreatedWithWarnings<T> {
  data: T;
  warnings: string[];
}
