using Zmg.Domain.Enums;

namespace Zmg.Domain.Entities;

public class TemplateTask
{
    public Guid Id { get; set; }
    public Guid ChecklistTemplateId { get; set; }
    public ChecklistTemplate? ChecklistTemplate { get; set; }
    public string Title { get; set; } = string.Empty;
    public Phase Phase { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// Stable identity for a seeded task (v2.8/M47) — see <see cref="TaskCodes"/>. Unique per template.
    /// <b>Null for a task the user added in the editor</b>, which is correct: that's user content, and
    /// it is simply never translated. Editing a seeded task's title clears it for the same reason.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>Per-locale overrides of <see cref="Title"/>; English is the column itself, never a row.</summary>
    public List<TemplateTaskTranslation> Translations { get; set; } = new();

    /// <summary>Task timeframe (v1.1). Copied onto each release task; see <see cref="ReleaseTask"/>.</summary>
    public int? MinDaysBefore { get; set; }
    public int? MaxDaysBefore { get; set; }
}
