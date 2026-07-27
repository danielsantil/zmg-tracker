using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain;

/// <summary>
/// The soft advisory warnings surfaced on a release (home card / releases table / detail header):
/// a missing UPC after distribution and the empty/thin-album nags. Pure and returned as a flat code
/// list, so the API ships a single <c>warnings</c> array rather than one boolean field per warning.
/// The codes are i18next key paths the SPA renders (M46); the same album codes drive the EmptyAlbum
/// pending action. None of them interpolate, so they stay plain strings rather than
/// <see cref="Message"/>s.
/// </summary>
public static class ReleaseWarnings
{
    public const string MissingUpc = "warning.missingUpc";
    public const string AlbumIsEmpty = "warning.albumIsEmpty";
    public const string AlbumHasOneTrack = "warning.albumHasOneTrack";

    public static List<string> Compute(
        ReleaseType type, int trackCount, bool isArchived, bool distributed, string? upc)
    {
        // Archived releases are terminal and read-only — no actionable advisories.
        if (isArchived) return [];

        var warnings = new List<string>();

        if (Release.NeedsWarning(distributed, upc))
            warnings.Add(MissingUpc);

        // Albums only: nag until they carry at least two tracks. Singles always have exactly one.
        if (type == ReleaseType.Album)
        {
            if (trackCount == 0) warnings.Add(AlbumIsEmpty);
            else if (trackCount == 1) warnings.Add(AlbumHasOneTrack);
        }

        return warnings;
    }
}
