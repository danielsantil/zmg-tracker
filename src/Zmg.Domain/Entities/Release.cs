using Zmg.Domain.Enums;

namespace Zmg.Domain.Entities;

/// <summary>
/// A single or album release. The unit a checklist is copied onto.
/// UPC/ISRC hang phase-2 streaming/revenue data off a stable id; keep the id stable.
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

    /// <summary>Release identifiers (v1.1). Optional free text, no format validation; blank until DSP distribution.</summary>
    public string? Upc { get; set; }
    public string? Isrc { get; set; }

    /// <summary>
    /// Archive lifecycle (v1.2). <see cref="ArchivedAt"/> is set when a release is archived (terminal,
    /// non-restorable — see build-plan-1.2.md); <see cref="DeletedAt"/> is a soft-delete stamp set when an
    /// archived release is removed. Releases are never hard-deleted; a global query filter hides removed rows.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>True while the release is archived (and not removed).</summary>
    public bool IsArchived => ArchivedAt is not null;

    public List<ReleaseArtist> FeaturedArtists { get; set; } = new();
    public List<ReleaseTask> Tasks { get; set; } = new();
    public List<Track> Tracks { get; set; } = new();
    
    /// <summary>True once the release's "Distribute to DSPs" task is checked.</summary>
    public bool IsDistributed => Tasks.Any(t => t is { Title: SeedData.DistributeToDspsTitle, IsDone: true });

    /// <summary>
    /// Whether a soft identifier warning should show: distributed, and UPC or ISRC still blank.
    /// Advisory only — never blocks a save.
    /// </summary>
    public static bool NeedsWarning(bool distributed, string? upc, string? isrc) =>
        distributed && (string.IsNullOrWhiteSpace(upc) || string.IsNullOrWhiteSpace(isrc));

    /// <summary>
    /// A human label for which identifiers are missing (e.g. "Missing UPC, ISRC"), or null when both
    /// are present. Independent of distribution state; callers gate on <see cref="NeedsWarning"/>.
    /// </summary>
    public string? MissingLabel()
    {
        var missing = new List<string>(2);
        if (string.IsNullOrWhiteSpace(Upc)) missing.Add("UPC");
        if (string.IsNullOrWhiteSpace(Isrc)) missing.Add("ISRC");
        return missing.Count == 0 ? null : "Missing " + string.Join(", ", missing);
    }
}