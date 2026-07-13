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

export interface ReleaseTaskDto {
  id: string;
  title: string;
  phase: Phase;
  sortOrder: number;
  isDone: boolean;
  completedAt: string | null;
  notes: string | null;
  minDaysBefore: number | null;
  maxDaysBefore: number | null;
}

export interface PhaseGroup {
  phase: Phase;
  done: number;
  total: number;
  tasks: ReleaseTaskDto[];
}

export interface TrackDto {
  id: string;
  trackNumber: number;
  title: string;
  isFocusTrack: boolean;
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

// Pending-actions engine (M10). Enum serialized as int by the API.
export enum PendingKind {
  TaskDue = 0,
  MissingIdentifier = 1,
}

export interface PendingAction {
  releaseId: string;
  releaseTitle: string;
  artistName: string;
  kind: PendingKind;
  taskId: string | null;
  label: string;
  daysToRelease: number | null;
}

// Templates (M3 template management)
export interface TemplateTaskDto {
  id: string;
  title: string;
  phase: Phase;
  sortOrder: number;
  minDaysBefore: number | null;
  maxDaysBefore: number | null;
}

export interface TemplatePhaseGroup {
  phase: Phase;
  tasks: TemplateTaskDto[];
}

export interface Template {
  id: string;
  type: ReleaseType;
  phases: TemplatePhaseGroup[];
}
