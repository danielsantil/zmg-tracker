namespace Zmg.Domain.Entities;

/// <summary>
/// A recording artist. DSP identifiers (Spotify artist id, etc.) hang off this
/// in a later phase; keep the id stable.
/// </summary>
public class Artist
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public List<Release> Releases { get; set; } = new();

    /// <summary>Songs this artist is the main artist of (v2.0).</summary>
    public List<Song> Songs { get; set; } = new();

    /// <summary>Songs this artist is credited on as a feat/collab (v2.0).</summary>
    public List<SongArtist> SongCredits { get; set; } = new();
}