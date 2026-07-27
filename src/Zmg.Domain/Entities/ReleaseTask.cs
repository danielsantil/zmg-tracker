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
    /// renumbering hazard can invalidate and a deleted template task orphans outright. Identity only:
    /// it answers "which seeded task is this" for <see cref="Release.IsDistributed"/>, and is
    /// deliberately **not** what per-locale text resolves through — see <see cref="Translations"/>.
    /// <b>Cleared when the user edits the title</b>: they have overridden the standard text, so the
    /// task is theirs from then on.
    /// </summary>
    public string? SourceCode { get; set; }

    /// <summary>
    /// This task's non-English text, copied down from the template at creation. The release owns it,
    /// exactly as it owns <see cref="Title"/> — resolving from the template's rows instead would let a
    /// template edit rewrite live checklists (see <see cref="ReleaseTaskTranslation"/>).
    /// </summary>
    public List<ReleaseTaskTranslation> Translations { get; set; } = new();
}