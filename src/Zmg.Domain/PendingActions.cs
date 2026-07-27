using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain;

/// <summary>
/// One thing the user should act on soon. Data-keyed, not tied to specific task titles.
/// An action is owned by either a release (<see cref="ReleaseId"/>) or a song (<see cref="SongId"/>);
/// <see cref="Subject"/> is that owner's display name (release title / song title).
/// </summary>
/// <remarks>
/// The text fields are split by <see cref="Kind"/> and each means exactly one thing (v2.9). The three
/// data kinds carry a <see cref="WarningCode"/> the SPA runs through <c>t()</c>;
/// <see cref="PendingKind.TaskDue"/> carries the task's own text in
/// <see cref="TaskTitleEn"/>/<see cref="TaskTitleEs"/> — user content, rendered verbatim in whichever
/// language the reader has selected. Exactly one of the two groups is populated on any given action.
/// <para>
/// This replaces M46's single <c>Label</c>, which was those two things overloaded onto one field. The
/// server no longer picks the language: both texts go on the wire and the SPA reads the column it
/// wants, so nothing here needs a locale, a culture, or a refetch when the user switches.
/// </para>
/// </remarks>
public record PendingAction(
    PendingKind Kind,
    string? WarningCode,
    string? TaskTitleEn,
    string? TaskTitleEs,
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
    /// <summary>The one advisory code this engine owns; the other two come from <see cref="ReleaseWarnings"/>.</summary>
    public const string MissingIsrc = "warning.missingIsrc";

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
                    // Both texts, straight off the release's own snapshot columns, so this row reads
                    // exactly like the checklist does in whichever language the reader picks.
                    PendingKind.TaskDue, null, t.TitleEn, t.TitleEs,
                    release.Title, artistName, release.Id, null, t.Id, daysToRelease));
            }
        }

        // 2. Missing UPC — one action per release once distributed with a blank UPC.
        if (release.IsDistributed && string.IsNullOrWhiteSpace(release.Upc))
        {
            result.Add(new PendingAction(
                PendingKind.MissingUpc, ReleaseWarnings.MissingUpc, null, null, release.Title, artistName,
                release.Id, null, null, null));
        }

        // 3. Empty album — every non-archived album with fewer than two tracks (released ones included);
        // the nag persists until the tracks exist. Singles never qualify (they carry exactly one track).
        if (release is { Type: ReleaseType.Album, IsArchived: false } && release.Tracks.Count < 2)
        {
            var code = release.Tracks.Count == 0 ? ReleaseWarnings.AlbumIsEmpty : ReleaseWarnings.AlbumHasOneTrack;
            result.Add(new PendingAction(
                PendingKind.EmptyAlbum, code, null, null, release.Title, artistName,
                release.Id, null, null, null));
        }

        return result;
    }

    /// <summary>
    /// Song-owned pending action: a missing ISRC once the song is distributed. A song counts as distributed
    /// when any linked, non-deleted, non-archived release has its DSP-distribution task checked (by code,
    /// not title, since M47) — the
    /// caller precomputes that flag, so this yields exactly one action per song, never per release.
    /// </summary>
    public static List<PendingAction> ComputeForSong(Song song, bool hasDistributedRelease)
    {
        var result = new List<PendingAction>();

        if (hasDistributedRelease && !song.IsArchived && string.IsNullOrWhiteSpace(song.Isrc))
        {
            result.Add(new PendingAction(
                PendingKind.MissingIsrc, MissingIsrc, null, null, song.Title,
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
