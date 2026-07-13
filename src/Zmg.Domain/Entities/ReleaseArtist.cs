using Zmg.Domain.Enums;

namespace Zmg.Domain.Entities;

/// <summary>
/// Join between a release and a featured/collab artist. The main artist stays a
/// direct FK on <see cref="Release"/>.
/// </summary>
public class ReleaseArtist
{
    public Guid ReleaseId { get; set; }
    public Release? Release { get; set; }
    public Guid ArtistId { get; set; }
    public Artist? Artist { get; set; }
    public ArtistRole Role { get; set; }
}