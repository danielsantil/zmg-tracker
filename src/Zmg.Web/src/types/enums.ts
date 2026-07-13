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

// Pending-actions engine (M10). Enum serialized as int by the API.
export enum PendingKind {
  TaskDue = 0,
  MissingIdentifier = 1,
}
