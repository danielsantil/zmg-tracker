namespace Zmg.Domain.Entities;

/// <summary>
/// A song is the creative work (v2.0). It carries the ISRC, its own feats/collabs, and its own
/// archive lifecycle. It links to releases through <see cref="Track"/>, so one song can sit on a
/// single and later on an album. UPCs and release date are derived from those links, never stored.
/// No cover, no tasks — those belong to releases.
/// </summary>
public class Song
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Main artist. Copied from the release at inline creation, then independent and editable.</summary>
    public Guid MainArtistId { get; set; }
    public Artist? MainArtist { get; set; }

    /// <summary>Optional free text, same policy as <see cref="Release.Upc"/> — blank until DSP distribution.</summary>
    public string? Isrc { get; set; }

    /// <summary>
    /// Archive lifecycle (mirrors <see cref="Release"/>). <see cref="ArchivedAt"/> is set on archive
    /// (terminal, read-only); the lifecycle wiring lands in M15. Removing an archived-or-orphan song
    /// hard-deletes the row (M36).
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>Feats/collabs on the song (main artist stays a direct FK).</summary>
    public List<SongArtist> Artists { get; set; } = new();

    /// <summary>Release links (join rows). A song's UPCs and release date derive from these.</summary>
    public List<Track> ReleaseLinks { get; set; } = new();

    /// <summary>True while the song is archived (and not removed).</summary>
    public bool IsArchived => ArchivedAt is not null;
}
