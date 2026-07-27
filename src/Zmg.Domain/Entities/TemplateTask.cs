using Zmg.Domain.Enums;

namespace Zmg.Domain.Entities;

public class TemplateTask
{
    public Guid Id { get; set; }
    public Guid ChecklistTemplateId { get; set; }
    public ChecklistTemplate? ChecklistTemplate { get; set; }

    /// <summary>The task text in English. Required — it is the fallback whenever Spanish is blank.</summary>
    public string TitleEn { get; set; } = string.Empty;

    /// <summary>
    /// The task text in Spanish, or null to show <see cref="TitleEn"/> to Spanish readers too (v2.9).
    /// Null is a valid, deliberate state rather than a gap: it is the honest way to say "this reads the
    /// same in both languages" instead of storing a copy of the English and keeping the two in step.
    /// </summary>
    public string? TitleEs { get; set; }

    public Phase Phase { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// Stable identity for a seeded task — see <see cref="TaskCodes"/>. <b>Identity only</b> (v2.9):
    /// rules key off it and nothing else does. It is deliberately <i>not</i> how text is looked up, so
    /// editing a title leaves it untouched. <b>Null for a task the user added in the editor</b>, which
    /// is correct — no rule can be about a task that didn't exist when the rule was written.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>Task timeframe (v1.1). Copied onto each release task; see <see cref="ReleaseTask"/>.</summary>
    public int? MinDaysBefore { get; set; }
    public int? MaxDaysBefore { get; set; }
}
