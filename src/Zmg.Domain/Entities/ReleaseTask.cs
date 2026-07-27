using Zmg.Domain.Enums;

namespace Zmg.Domain.Entities;

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

    /// <summary>
    /// Task timeframe (v1.1), copied from the template task. Both nullable, mostly null.
    /// Pre tasks: "complete N–M days before release" (max drives pending/sort, min display-only).
    /// Release/Post tasks: "days to complete" after release — stored, not acted on yet.
    /// </summary>
    public int? MinDaysBefore { get; set; }
    public int? MaxDaysBefore { get; set; }

    /// <summary>Lineage back to the template task it was copied from. Nothing depends on it in v1.</summary>
    public Guid? SourceTemplateTaskId { get; set; }

    /// <summary>
    /// The <see cref="TaskCodes"/> slug this task was stamped with at copy time (v2.8/M47) — the
    /// lineage that actually survives, unlike <see cref="SourceTemplateTaskId"/>, which the seed-data
    /// renumbering hazard can invalidate and a deleted template task orphans outright. Drives
    /// per-locale text resolution and <see cref="Release.IsDistributed"/>.
    /// <b>Cleared when the user edits the title</b>: they have overridden the standard text, and a
    /// translation silently reverting their edit on a language switch would be a bug.
    /// </summary>
    public string? SourceCode { get; set; }
}