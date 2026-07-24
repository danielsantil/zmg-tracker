export interface Artist {
  id: string;
  name: string;
  notes: string | null;
  // Active (non-archived) references — shown in the table and blocking deletion up front.
  releaseCount: number;
  songCount: number;
  /** Feat/collab appearances on active songs (also block deletion). */
  creditCount: number;
  // Archived data a delete would cascade-remove — drives the confirm-dialog warning, not the block.
  archivedReleaseCount: number;
  archivedSongCount: number;
}

export interface ArtistInput {
  name: string;
  notes: string | null;
}
