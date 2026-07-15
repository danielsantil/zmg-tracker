using Zmg.Domain.Enums;

namespace Zmg.Domain.Entities;

/// <summary>
/// Join between a song and a featured/collab artist (v2.0). Replaces the old ReleaseArtist —
/// feats/collabs live on the SONG now. The main artist stays a direct FK on <see cref="Song"/>.
/// </summary>
public class SongArtist
{
    public Guid SongId { get; set; }
    public Song? Song { get; set; }
    public Guid ArtistId { get; set; }
    public Artist? Artist { get; set; }
    public ArtistRole Role { get; set; }
}
