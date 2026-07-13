using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

public class ProgressTests
{
    private static ReleaseTask Task(Phase phase, bool done) =>
        new() { Id = Guid.NewGuid(), Phase = phase, IsDone = done };

    [Fact]
    public void Count_reports_done_over_total()
    {
        var tasks = new[] { Task(Phase.Pre, true), Task(Phase.Pre, false), Task(Phase.Post, true) };

        var count = ProgressCalculator.Count(tasks);

        Assert.Equal(2, count.Done);
        Assert.Equal(3, count.Total);
        Assert.Equal(67, count.Percent);
    }

    [Fact]
    public void Empty_task_list_is_zero_percent_not_a_divide_by_zero()
    {
        var count = ProgressCalculator.Count(Array.Empty<ReleaseTask>());

        Assert.Equal(0, count.Total);
        Assert.Equal(0, count.Percent);
        Assert.Equal(0d, count.Fraction);
    }

    [Fact]
    public void Calculate_breaks_progress_down_per_phase()
    {
        var tasks = new[]
        {
            Task(Phase.Pre, true), Task(Phase.Pre, true),
            Task(Phase.Release, true), Task(Phase.Release, false),
            Task(Phase.Post, false),
        };

        var progress = ProgressCalculator.Calculate(tasks);

        Assert.Equal(new ProgressCount(3, 5), progress.Overall);
        Assert.Equal(new ProgressCount(2, 2), progress.ByPhase[Phase.Pre]);
        Assert.Equal(new ProgressCount(1, 2), progress.ByPhase[Phase.Release]);
        Assert.Equal(new ProgressCount(0, 1), progress.ByPhase[Phase.Post]);
    }
}

public class ReleaseStatusTests
{
    private static readonly DateOnly Today = new(2026, 7, 11);

    [Fact]
    public void Future_date_with_open_tasks_is_upcoming()
    {
        var status = ReleaseStatus.Derive(Today.AddDays(30), Today, new ProgressCount(0, 30));
        Assert.Equal(ReleaseStatus.Upcoming, status);
    }

    [Fact]
    public void Past_date_with_open_tasks_is_released()
    {
        var status = ReleaseStatus.Derive(Today.AddDays(-1), Today, new ProgressCount(5, 30));
        Assert.Equal(ReleaseStatus.Released, status);
    }

    [Fact]
    public void All_tasks_done_is_complete_regardless_of_date()
    {
        var future = ReleaseStatus.Derive(Today.AddDays(30), Today, new ProgressCount(30, 30));
        var past = ReleaseStatus.Derive(Today.AddDays(-30), Today, new ProgressCount(30, 30));
        Assert.Equal(ReleaseStatus.Complete, future);
        Assert.Equal(ReleaseStatus.Complete, past);
    }

    [Fact]
    public void Release_day_counts_as_released()
    {
        var status = ReleaseStatus.Derive(Today, Today, new ProgressCount(0, 30));
        Assert.Equal(ReleaseStatus.Released, status);
    }
}
