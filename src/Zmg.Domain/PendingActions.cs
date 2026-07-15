using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain;

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
    public static List<PendingAction> Compute(Release release, DateOnly today)
    {
        var artistName = release.MainArtist?.Name ?? string.Empty;
        var daysToRelease = release.ReleaseDate.DayNumber - today.DayNumber;
        var result = new List<PendingAction>();

        // 1. Task due — incomplete task with a timeframe (max drives), window open, not yet released.
        foreach (var t in release.Tasks
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

        // 2. Missing identifier — one action per release once distributed with a blank UPC.
        // v2.0: ISRC moved to the song, so this is UPC-only here; the ISRC nag is reworked in M14.
        if (release.IsDistributed && string.IsNullOrWhiteSpace(release.Upc))
        {
            result.Add(new PendingAction(
                release.Id, release.Title, artistName,
                PendingKind.MissingIdentifier, null, "Missing UPC", null));
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
