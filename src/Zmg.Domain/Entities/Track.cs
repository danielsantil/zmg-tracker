namespace Zmg.Domain.Entities;

/// <summary>
/// A pure Release↔Song join (v2.0). Composite PK <c>(ReleaseId, SongId)</c> — no surrogate id —
/// structurally prevents the same song appearing twice on one release. The title/ISRC/feats live
/// on <see cref="Song"/>; only ordering and focus live here. TrackNumber stays 1-based and
/// contiguous per release.
/// </summary>
public class Track
{
    public Guid ReleaseId { get; set; }
    public Release? Release { get; set; }
    public Guid SongId { get; set; }
    public Song? Song { get; set; }
    public int TrackNumber { get; set; }
    public bool IsFocusTrack { get; set; }
}
