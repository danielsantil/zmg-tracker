namespace Zmg.Domain;

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

/// <summary>
/// A single or album release. The unit a checklist is copied onto.
/// UPC/ISRC-ready metadata is added in phase 2; keep the id stable.
/// </summary>
public class Release
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ReleaseType Type { get; set; }
    public DateOnly ReleaseDate { get; set; }
    public Guid MainArtistId { get; set; }
    public Artist? MainArtist { get; set; }
    public string? CoverUrl { get; set; }
    public string? Notes { get; set; }

    public List<ReleaseArtist> FeaturedArtists { get; set; } = new();
    public List<ReleaseTask> Tasks { get; set; } = new();
    public List<Track> Tracks { get; set; } = new();
}

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

/// <summary>
/// A live checklist task owned by a release. Copied from a template on creation,
/// then freely edited without affecting the template it came from.
/// </summary>
public class ReleaseTask
{
    public Guid Id { get; set; }
    public Guid ReleaseId { get; set; }
    public Release? Release { get; set; }
    public string Title { get; set; } = string.Empty;
    public Phase Phase { get; set; }
    public int SortOrder { get; set; }
    public bool IsDone { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    /// <summary>Lineage back to the template task it was copied from. Nothing depends on it in v1.</summary>
    public Guid? SourceTemplateTaskId { get; set; }
}

/// <summary>
/// The editable default checklist for a release type. One per <see cref="ReleaseType"/>.
/// Edits never touch existing releases (releases own a snapshot copy).
/// </summary>
public class ChecklistTemplate
{
    public Guid Id { get; set; }
    public ReleaseType Type { get; set; }
    public List<TemplateTask> Tasks { get; set; } = new();
}

public class TemplateTask
{
    public Guid Id { get; set; }
    public Guid ChecklistTemplateId { get; set; }
    public ChecklistTemplate? ChecklistTemplate { get; set; }
    public string Title { get; set; } = string.Empty;
    public Phase Phase { get; set; }
    public int SortOrder { get; set; }
}
