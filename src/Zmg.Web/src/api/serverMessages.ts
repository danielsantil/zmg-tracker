/**
 * Server error messages the SPA has to recognise, mirrored here as the single TS-side source of truth.
 * These must match the C# constants exactly (they ship no error code) — change both sides together.
 */

/** Mirrors `Validation.DuplicateSongTitleMessage` (Zmg.Domain/Validation.cs). */
export const DUPLICATE_SONG_TITLE_MESSAGE =
  'A song with this title already exists for this artist.';
