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
