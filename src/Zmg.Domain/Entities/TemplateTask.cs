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

    /// <summary>Task timeframe (v1.1). Copied onto each release task; see <see cref="ReleaseTask"/>.</summary>
    public int? MinDaysBefore { get; set; }
    public int? MaxDaysBefore { get; set; }
}
