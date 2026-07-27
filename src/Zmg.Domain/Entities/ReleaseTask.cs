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

    /// <summary>The task text in English. Required — it is the fallback whenever Spanish is blank.</summary>
    public string TitleEn { get; set; } = string.Empty;

    /// <summary>
    /// The task text in Spanish, or null to show <see cref="TitleEn"/> to Spanish readers too (v2.9).
    /// The release owns both columns outright — they are copied down at create, and a later template
    /// edit cannot reach them. That is what makes the snapshot rule hold in <i>every</i> language.
    /// </summary>
    public string? TitleEs { get; set; }

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
    /// The <see cref="TaskCodes"/> slug this task was stamped with at copy time — the lineage that
    /// actually survives, unlike <see cref="SourceTemplateTaskId"/>, which a deleted template task
    /// orphans outright. <b>Identity only</b>, answering exactly one question — "which seeded task is
    /// this?" — for <see cref="Release.IsDistributed"/>.
    /// <para>
    /// <b>Editing the title does not clear it (v2.9)</b>, and that is the point. While the code doubled
    /// as a translation join key it had to be dropped on every edit, so rewording "Distribute to DSPs"
    /// on a release silently switched off the missing-UPC warning, the pending engine and the past-date
    /// backfill for that release. Text and identity are separate now: rewording is just rewording.
    /// </para>
    /// </summary>
    public string? SourceCode { get; set; }
}
