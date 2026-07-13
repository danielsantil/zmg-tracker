namespace Zmg.Domain.Entities;

/// <summary>
/// A track on a release. Albums only in practice; singles skip it in v1.
/// </summary>
public class Track
{
    public Guid Id { get; set; }
    public Guid ReleaseId { get; set; }
    public Release? Release { get; set; }
    public int TrackNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsFocusTrack { get; set; }
}