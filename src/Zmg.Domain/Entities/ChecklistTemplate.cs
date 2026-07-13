using Zmg.Domain.Enums;

namespace Zmg.Domain.Entities;

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