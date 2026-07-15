using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain;

/// <summary>
/// One thing the user should act on soon. Data-keyed, not tied to specific task titles.
/// An action is owned by either a release (<see cref="ReleaseId"/>) or a song (<see cref="SongId"/>);
/// <see cref="Subject"/> is that owner's display name (release title / song title).
/// </summary>
public record PendingAction(
    PendingKind Kind,
    string Label,
    string Subject,
    string ArtistName,
    Guid? ReleaseId,
    Guid? SongId,
    Guid? TaskId,
    int? DaysToRelease);

/// <summary>
/// The pending-actions engine (v1.1 M10; reworked v2.0 M14). Pure and reused by <c>GET /api/pending</c>
/// (aggregate) and the release-detail "Needs attention" block. Keyed off data (a task's timeframe, a
/// distributed release's blank UPC, a distributed song's blank ISRC, an under-filled album), so adding a
/// timeframe to any task later makes it participate with no code change.
/// </summary>
public static class PendingActions
{
    /// <summary>
    /// Release-owned pending actions: task-due items (in phase order), a missing-UPC nag once distributed,
    /// and an empty-album nag. Song-owned ISRC actions come from <see cref="ComputeForSong"/>. The aggregate
    /// ordering across owners is applied by <see cref="Order"/>.
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
                    PendingKind.TaskDue, t.Title, release.Title, artistName,
                    release.Id, null, t.Id, daysToRelease));
            }
        }

        // 2. Missing UPC — one action per release once distributed with a blank UPC.
        if (release.IsDistributed && string.IsNullOrWhiteSpace(release.Upc))
        {
            result.Add(new PendingAction(
                PendingKind.MissingUpc, "Missing UPC", release.Title, artistName,
                release.Id, null, null, null));
        }

        // 3. Empty album — every non-archived album with fewer than two tracks (released ones included);
        // the nag persists until the tracks exist. Singles never qualify (they carry exactly one track).
        if (release is { Type: ReleaseType.Album, IsArchived: false } && release.Tracks.Count < 2)
        {
            var label = release.Tracks.Count == 0 ? "Album is empty" : "Album has only 1 track";
            result.Add(new PendingAction(
                PendingKind.EmptyAlbum, label, release.Title, artistName,
                release.Id, null, null, null));
        }

        return result;
    }

    /// <summary>
    /// Song-owned pending action: a missing ISRC once the song is distributed. A song counts as distributed
    /// when any linked, non-deleted, non-archived release has its "Distribute to DSPs" task checked — the
    /// caller precomputes that flag, so this yields exactly one action per song, never per release.
    /// </summary>
    public static List<PendingAction> ComputeForSong(Song song, bool hasDistributedRelease)
    {
        var result = new List<PendingAction>();

        if (hasDistributedRelease && !song.IsArchived && string.IsNullOrWhiteSpace(song.Isrc))
        {
            result.Add(new PendingAction(
                PendingKind.MissingIsrc, "Missing ISRC", song.Title,
                song.MainArtist?.Name ?? string.Empty,
                null, song.Id, null, null));
        }

        return result;
    }

    /// <summary>
    /// Global ordering for the aggregate list: all task-due items first, nearest release date on top
    /// (ascending days-to-release); then the data kinds (missing UPC/ISRC, empty album) by subject.
    /// </summary>
    public static List<PendingAction> Order(IEnumerable<PendingAction> actions)
    {
        var list = actions.ToList();
        return list
            .Where(a => a.Kind == PendingKind.TaskDue)
            .OrderBy(a => a.DaysToRelease)
            .Concat(list
                .Where(a => a.Kind != PendingKind.TaskDue)
                .OrderBy(a => a.Subject, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
