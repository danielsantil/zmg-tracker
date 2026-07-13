using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

/// <summary>M10 — the pure pending-actions engine (strongest v1.1 signal).</summary>
public class PendingActionsTests
{
    private static readonly DateOnly Today = new(2026, 7, 12);

    private static Release Rel(DateOnly date, string title = "Song", string? upc = null, string? isrc = null,
        params ReleaseTask[] tasks) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            ReleaseDate = date,
            MainArtist = new Artist { Name = "Artist" },
            Upc = upc,
            Isrc = isrc,
            Tasks = tasks.ToList(),
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
    public void Missing_identifier_pends_only_after_distribution()
    {
        // Distribute not done → no missing-id action even with blank ids.
        var notDist = Rel(Today.AddDays(-5),
            tasks: Task(SeedData.DistributeToDspsTitle, done: false));
        Assert.Empty(PendingActions.Compute(notDist, Today));

        // Distribute done, both ids blank → one missing-id action summarizing both.
        var dist = Rel(Today.AddDays(-5),
            tasks: Task(SeedData.DistributeToDspsTitle, done: true));
        var action = Assert.Single(PendingActions.Compute(dist, Today));
        Assert.Equal(PendingKind.MissingIdentifier, action.Kind);
        Assert.Equal("Missing UPC, ISRC", action.Label);
        Assert.Null(action.TaskId);

        // Both ids filled → no missing-id action.
        var filled = Rel(Today.AddDays(-5), upc: "u", isrc: "i",
            tasks: Task(SeedData.DistributeToDspsTitle, done: true));
        Assert.Empty(PendingActions.Compute(filled, Today));
    }

    [Fact]
    public void Order_puts_task_due_nearest_first_then_data_items_last()
    {
        // A far release with an open window, a near release with an open window,
        // and a distributed past release missing ids.
        var far = Rel(Today.AddDays(12), title: "Far", tasks: Task("Pitch to Spotify", min: 7, max: 14));
        var near = Rel(Today.AddDays(3), title: "Near", tasks: Task("Distribute to DSPs", min: 7, max: 14));
        var missing = Rel(Today.AddDays(-2), title: "Missing",
            tasks: Task(SeedData.DistributeToDspsTitle, done: true));

        var all = new[] { far, near, missing }
            .SelectMany(r => PendingActions.Compute(r, Today));
        var ordered = PendingActions.Order(all);

        Assert.Equal(3, ordered.Count);
        Assert.Equal(PendingKind.TaskDue, ordered[0].Kind);
        Assert.Equal("Near", ordered[0].ReleaseTitle);      // nearest release first
        Assert.Equal(PendingKind.TaskDue, ordered[1].Kind);
        Assert.Equal("Far", ordered[1].ReleaseTitle);
        Assert.Equal(PendingKind.MissingIdentifier, ordered[2].Kind); // data item last
        Assert.Equal("Missing", ordered[2].ReleaseTitle);
    }

    [Fact]
    public void Empty_when_nothing_is_pending()
    {
        var rel = Rel(Today.AddDays(10), "Song", null, null,
            Task("Mix/master"), Task("Design cover for DSPs", done: true));
        Assert.Empty(PendingActions.Compute(rel, Today));
    }
}
