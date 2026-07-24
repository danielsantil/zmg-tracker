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

    /// <summary>Release identifier (v1.1). Optional free text, no format validation; blank until DSP distribution.
    /// ISRC moved to <see cref="Song"/> in v2.0 — a release keeps only its UPC.</summary>
    public string? Upc { get; set; }

    /// <summary>
    /// Archive lifecycle (v1.2). <see cref="ArchivedAt"/> is set when a release is archived (terminal,
    /// non-restorable — see build-plan-1.2.md). Removing an archived release hard-deletes the row (M36);
    /// the FK cascade takes its tasks and track links with it.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>True while the release is archived (and not removed).</summary>
    public bool IsArchived => ArchivedAt is not null;

    public List<ReleaseTask> Tasks { get; set; } = new();
    public List<Track> Tracks { get; set; } = new();

    /// <summary>True once the release's "Distribute to DSPs" task is checked.</summary>
    public bool IsDistributed => Tasks.Any(t => t is { Title: SeedData.DistributeToDspsTitle, IsDone: true });

    /// <summary>
    /// Whether a soft identifier warning should show: distributed, and the UPC still blank (v2.0 —
    /// ISRC now hangs off the song, so the release-level warning is UPC-only). Advisory, never blocks.
    /// </summary>
    public static bool NeedsWarning(bool distributed, string? upc) =>
        distributed && string.IsNullOrWhiteSpace(upc);
}