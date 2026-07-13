using Zmg.Domain.Entities;

namespace Zmg.Domain;

/// <summary>
/// Pure template-copy logic. No I/O: the API layer loads the template and persists
/// the result; this just maps template tasks to fresh release tasks.
/// </summary>
public static class TemplateCopy
{
    /// <summary>
    /// Snapshot the template's tasks onto a release. Preserves title, phase and order,
    /// sets <see cref="ReleaseTask.IsDone"/> false, and records lineage back to the
    /// template task. The release owns the result from here on.
    /// </summary>
    public static List<ReleaseTask> CopyToRelease(ChecklistTemplate template, Guid releaseId)
    {
        ArgumentNullException.ThrowIfNull(template);

        return template.Tasks
            .OrderBy(t => t.Phase)
            .ThenBy(t => t.SortOrder)
            .Select(t => new ReleaseTask
            {
                Id = Guid.NewGuid(),
                ReleaseId = releaseId,
                Title = t.Title,
                Phase = t.Phase,
                SortOrder = t.SortOrder,
                IsDone = false,
                CompletedAt = null,
                Notes = null,
                MinDaysBefore = t.MinDaysBefore,
                MaxDaysBefore = t.MaxDaysBefore,
                SourceTemplateTaskId = t.Id
            })
            .ToList();
    }
}
