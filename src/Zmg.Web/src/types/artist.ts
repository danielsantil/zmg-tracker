export interface Artist {
  id: string;
  name: string;
  notes: string | null;
  releaseCount: number;
  songCount: number;
  /** Feat/collab appearances on songs (also block deletion). */
  creditCount: number;
}

export interface ArtistInput {
  name: string;
  notes: string | null;
}
