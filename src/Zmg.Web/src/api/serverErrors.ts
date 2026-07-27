/**
 * Server error codes the SPA has to *recognise* — not render (M46 retired `serverMessages.ts`, which
 * mirrored the C# prose verbatim because the wire shipped no code). Only codes the UI branches on
 * belong here; everything else is rendered straight from `ApiError.messages` and needs no constant.
 *
 * These mirror the C# constants and are permanent identifiers — change both sides together, same
 * rule as the integer enums.
 */
export const ServerErrors = {
  /** `Validation.DuplicateSongTitleCode` (Zmg.Domain/Validation.cs). */
  duplicateSongTitle: 'error.song.duplicateTitle',
} as const;
