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
    public List<ReleaseArtist> ReleaseCredits { get; set; } = new();
}