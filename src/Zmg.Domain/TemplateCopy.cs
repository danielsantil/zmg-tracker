using Zmg.Domain.Entities;

namespace Zmg.Domain;

/// <summary>
/// Pure template-copy logic. No I/O: the API layer loads the template and persists
/// the result; this just maps template tasks to fresh release tasks.
/// </summary>
public static class TemplateCopy
{
    /// <summary>
    /// Snapshot the template's tasks onto a release. Preserves <b>both</b> titles, phase and order,
    /// sets <see cref="ReleaseTask.IsDone"/> false, and records lineage back to the template task.
    /// The release owns the result from here on.
    /// </summary>
    /// <remarks>
    /// Both languages are plain columns, so the snapshot is a straight copy and the rule "editing a
    /// template shapes future releases only" holds in every language for free (v2.9). It used to take a
    /// child collection to copy and a lookup to resolve — and when the resolve read the *template's*
    /// rows, a template's Spanish edit rewrote the Spanish of every existing release while their
    /// English, snapshotted here, stayed put.
    /// </remarks>
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
                TitleEn = t.TitleEn,
                TitleEs = t.TitleEs,
                Phase = t.Phase,
                SortOrder = t.SortOrder,
                IsDone = false,
                CompletedAt = null,
                Notes = null,
                MinDaysBefore = t.MinDaysBefore,
                MaxDaysBefore = t.MaxDaysBefore,
                SourceTemplateTaskId = t.Id,
                // Stable lineage: the GUID alone doesn't survive a deleted or renumbered template
                // task, and IsDistributed keys off this.
                SourceCode = t.Code,
            })
            .ToList();
    }
}
