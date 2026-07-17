using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

/// <summary>M10 — the pure pending-actions engine (strongest v1.1 signal), reworked in M14.</summary>
public class PendingActionsTests
{
    private static readonly DateOnly Today = TestDates.Today;

    private static Release Rel(DateOnly date, string title = "Song", string? upc = null,
        ReleaseType type = ReleaseType.Single, params ReleaseTask[] tasks) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Type = type,
            ReleaseDate = date,
            MainArtist = new Artist { Name = "Artist" },
            Upc = upc,
            Tasks = tasks.ToList(),
            // A single carries exactly one track; an album's tracklist is set per test.
            Tracks = type == ReleaseType.Single ? new List<Track> { new() } : new List<Track>(),
        };

    private static ReleaseTask Task(string title, Phase phase = Phase.Pre, bool done = false,
        int? min = null, int? max = null, int sort = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Phase = phase,
            SortOrder = sort,
            IsDone = done,
            MinDaysBefore = min,
            MaxDaysBefore = max,
        };

    [Fact]
    public void Task_with_no_timeframe_never_pends()
    {
        var rel = Rel(Today.AddDays(3), tasks: Task("Mix/master"));
        Assert.Empty(PendingActions.Compute(rel, Today));
    }

    [Fact]
    public void Timeframe_task_pends_only_once_the_window_has_opened()
    {
        // max=14: window opens 14 days before release. Release is 20 days out → still closed.
        var closed = Rel(Today.AddDays(20), tasks: Task("Pitch to Spotify", min: 7, max: 14));
        Assert.Empty(PendingActions.Compute(closed, Today));

        // Release 10 days out → window open.
        var open = Rel(Today.AddDays(10), tasks: Task("Pitch to Spotify", min: 7, max: 14));
        var actions = PendingActions.Compute(open, Today);
        var due = Assert.Single(actions);
        Assert.Equal(PendingKind.TaskDue, due.Kind);
        Assert.Equal("Pitch to Spotify", due.Label);
        Assert.Equal(10, due.DaysToRelease);
    }

    [Fact]
    public void Completed_timeframe_task_does_not_pend()
    {
        var rel = Rel(Today.AddDays(10), tasks: Task("Pitch to Spotify", done: true, min: 7, max: 14));
        Assert.Empty(PendingActions.Compute(rel, Today));
    }

    [Fact]
    public void Timeframe_task_stops_pending_once_released()
    {
        // Window is open but the release date has passed → not a task-due action.
        var rel = Rel(Today.AddDays(-1), tasks: Task("Pitch to Spotify", min: 7, max: 14));
        Assert.DoesNotContain(PendingActions.Compute(rel, Today), a => a.Kind == PendingKind.TaskDue);
    }

    [Fact]
    public void Missing_upc_pends_only_after_distribution()
    {
        // Distribute not done → no missing-UPC action even with blank UPC.
        var notDist = Rel(Today.AddDays(-5),
            tasks: Task(SeedData.DistributeToDspsTitle, done: false));
        Assert.Empty(PendingActions.Compute(notDist, Today));

        // Distribute done, UPC blank → one missing-UPC action (release-owned).
        var dist = Rel(Today.AddDays(-5),
            tasks: Task(SeedData.DistributeToDspsTitle, done: true));
        var action = Assert.Single(PendingActions.Compute(dist, Today));
        Assert.Equal(PendingKind.MissingUpc, action.Kind);
        Assert.Equal("Missing UPC", action.Label);
        Assert.Null(action.TaskId);
        Assert.NotNull(action.ReleaseId);
        Assert.Null(action.SongId);

        // UPC filled → no missing-UPC action.
        var filled = Rel(Today.AddDays(-5), upc: "u",
            tasks: Task(SeedData.DistributeToDspsTitle, done: true));
        Assert.Empty(PendingActions.Compute(filled, Today));
    }

    [Theory]
    [InlineData(0, "Album is empty")]
    [InlineData(1, "Album has only 1 track")]
    public void Empty_album_pends_with_fewer_than_two_tracks(int trackCount, string label)
    {
        var album = Rel(Today.AddDays(10), type: ReleaseType.Album);
        album.Tracks = Enumerable.Range(0, trackCount).Select(_ => new Track()).ToList();

        var action = Assert.Single(PendingActions.Compute(album, Today), a => a.Kind == PendingKind.EmptyAlbum);
        Assert.Equal(label, action.Label);
        Assert.NotNull(action.ReleaseId);
    }

    [Fact]
    public void Full_album_does_not_pend_empty()
    {
        var album = Rel(Today.AddDays(10), type: ReleaseType.Album);
        album.Tracks = new List<Track> { new(), new() };
        Assert.DoesNotContain(PendingActions.Compute(album, Today), a => a.Kind == PendingKind.EmptyAlbum);
    }

    [Fact]
    public void Archived_album_does_not_pend_empty()
    {
        var album = Rel(Today.AddDays(10), type: ReleaseType.Album);
        album.ArchivedAt = DateTime.UtcNow;
        album.Tracks = new List<Track>();
        Assert.Empty(PendingActions.Compute(album, Today));
    }

    [Fact]
    public void Single_never_pends_empty_album()
    {
        // A single carries exactly one track and is never an EmptyAlbum candidate regardless.
        var single = Rel(Today.AddDays(10), type: ReleaseType.Single);
        single.Tracks = new List<Track>();
        Assert.DoesNotContain(PendingActions.Compute(single, Today), a => a.Kind == PendingKind.EmptyAlbum);
    }

    [Fact]
    public void Song_pends_missing_isrc_only_when_distributed_blank_and_active()
    {
        var song = new Song { Id = Guid.NewGuid(), Title = "Track", MainArtist = new Artist { Name = "Artist" } };

        // Not distributed → nothing (even with blank ISRC).
        Assert.Empty(PendingActions.ComputeForSong(song, hasDistributedRelease: false));

        // Distributed, blank ISRC → one song-owned action.
        var action = Assert.Single(PendingActions.ComputeForSong(song, hasDistributedRelease: true));
        Assert.Equal(PendingKind.MissingIsrc, action.Kind);
        Assert.Equal("Missing ISRC", action.Label);
        Assert.Equal(song.Id, action.SongId);
        Assert.Null(action.ReleaseId);

        // ISRC filled → nothing.
        song.Isrc = "US-ABC-00-00001";
        Assert.Empty(PendingActions.ComputeForSong(song, hasDistributedRelease: true));

        // Archived → nothing.
        song.Isrc = null;
        song.ArchivedAt = DateTime.UtcNow;
        Assert.Empty(PendingActions.ComputeForSong(song, hasDistributedRelease: true));
    }

    [Fact]
    public void Order_puts_task_due_nearest_first_then_data_items_by_subject()
    {
        // A far release with an open window, a near release with an open window,
        // and a distributed past release missing its UPC.
        var far = Rel(Today.AddDays(12), title: "Far", tasks: Task("Pitch to Spotify", min: 7, max: 14));
        var near = Rel(Today.AddDays(3), title: "Near", tasks: Task("Distribute to DSPs", min: 7, max: 14));
        var missing = Rel(Today.AddDays(-2), title: "Missing",
            tasks: Task(SeedData.DistributeToDspsTitle, done: true));

        var all = new[] { far, near, missing }
            .SelectMany(r => PendingActions.Compute(r, Today));
        var ordered = PendingActions.Order(all);

        Assert.Equal(3, ordered.Count);
        Assert.Equal(PendingKind.TaskDue, ordered[0].Kind);
        Assert.Equal("Near", ordered[0].Subject);      // nearest release first
        Assert.Equal(PendingKind.TaskDue, ordered[1].Kind);
        Assert.Equal("Far", ordered[1].Subject);
        Assert.Equal(PendingKind.MissingUpc, ordered[2].Kind); // data item last
        Assert.Equal("Missing", ordered[2].Subject);
    }

    [Fact]
    public void Empty_when_nothing_is_pending()
    {
        var rel = Rel(Today.AddDays(10), "Song", null,
            tasks: new[] { Task("Mix/master"), Task("Design cover for DSPs", done: true) });
        Assert.Empty(PendingActions.Compute(rel, Today));
    }
}
