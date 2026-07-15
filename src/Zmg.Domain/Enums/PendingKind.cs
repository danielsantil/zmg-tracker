namespace Zmg.Domain.Enums;

/// <summary>Why something is surfaced as needing attention (v1.1 M10; reworked v2.0 M14).</summary>
public enum PendingKind
{
    /// <summary>An incomplete task whose timeframe window has opened and the release hasn't shipped yet.</summary>
    TaskDue,
    /// <summary>A distributed release still missing its UPC (release-owned).</summary>
    MissingUpc,
    /// <summary>A distributed song still missing its ISRC (song-owned).</summary>
    MissingIsrc,
    /// <summary>A non-archived album with fewer than two tracks — nags until the tracklist is filled.</summary>
    EmptyAlbum,
}
