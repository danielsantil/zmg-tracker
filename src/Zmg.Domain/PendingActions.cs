namespace Zmg.Domain;

/// <summary>Why a release is surfaced as needing attention (v1.1 M10).</summary>
public enum PendingKind
{
    /// <summary>An incomplete task whose timeframe window has opened and the release hasn't shipped yet.</summary>
    TaskDue,
    /// <summary>A distributed release still missing its UPC and/or ISRC.</summary>
    MissingIdentifier,
}

/// <summary>One thing the user should act on soon. Data-keyed, not tied to specific task titles.</summary>
public record PendingAction(
    Guid ReleaseId,
    string ReleaseTitle,
    string ArtistName,
    PendingKind Kind,
    Guid? TaskId,
    string Label,
    int? DaysToRelease);

/// <summary>
/// The pending-actions engine (v1.1 M10). Pure and reused by <c>GET /api/pending</c> (aggregate)
/// and the release-detail "Needs attention" block. Keyed off data (a task's timeframe, a distributed
/// release's blank ids), so adding a timeframe to any task later makes it participate with no code change.
/// </summary>
public static class PendingActions
{
    /// <summary>
    /// Pending actions for a single release, already in per-release order (task-due items first in phase
    /// order, then the single missing-identifier item). The aggregate ordering across releases is applied
    /// by <see cref="Order"/>.
    /// </summary>
    public static List<PendingAction> Compute(Release release, IEnumerable<ReleaseTask> tasks, DateOnly today)
    {
        var taskList = tasks as ICollection<ReleaseTask> ?? tasks.ToList();
        var artistName = release.MainArtist?.Name ?? string.Empty;
        var daysToRelease = release.ReleaseDate.DayNumber - today.DayNumber;
        var result = new List<PendingAction>();

        // 1. Task due — incomplete task with a timeframe (max drives), window open, not yet released.
        foreach (var t in taskList
            .Where(t => !t.IsDone && t.MaxDaysBefore is not null)
            .OrderBy(t => t.Phase)
            .ThenBy(t => t.SortOrder))
        {
            var windowOpens = release.ReleaseDate.AddDays(-t.MaxDaysBefore!.Value);
            if (today >= windowOpens && release.ReleaseDate >= today)
            {
                result.Add(new PendingAction(
                    release.Id, release.Title, artistName,
                    PendingKind.TaskDue, t.Id, t.Title, daysToRelease));
            }
        }

        // 2. Missing identifier — one action per release once distributed with a blank id.
        if (IdentifierState.IsDistributed(taskList))
        {
            var label = IdentifierState.MissingLabel(release.Upc, release.Isrc);
            if (label is not null)
            {
                result.Add(new PendingAction(
                    release.Id, release.Title, artistName,
                    PendingKind.MissingIdentifier, null, label, null));
            }
        }

        return result;
    }

    /// <summary>
    /// Global ordering for the aggregate list: all task-due items first, nearest release date on top
    /// (ascending days-to-release); then all missing-identifier (data) items, grouped by release.
    /// </summary>
    public static List<PendingAction> Order(IEnumerable<PendingAction> actions)
    {
        var list = actions.ToList();
        return list
            .Where(a => a.Kind == PendingKind.TaskDue)
            .OrderBy(a => a.DaysToRelease)
            .Concat(list
                .Where(a => a.Kind == PendingKind.MissingIdentifier)
                .OrderBy(a => a.ReleaseTitle, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
