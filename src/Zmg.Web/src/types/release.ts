import type { ReleaseType } from './enums';
import type { PhaseGroup } from './task';
import type { TrackDto, TrackInput } from './track';

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
  status: string;
  upc: string | null;
  needsIdentifierWarning: boolean;
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
  doneTasks: number;
  totalTasks: number;
  phases: PhaseGroup[];
  tracks: TrackDto[];
  upc: string | null;
  needsIdentifierWarning: boolean;
  isArchived: boolean;
}

export interface CreatedWithWarnings<T> {
  data: T;
  warnings: string[];
}
