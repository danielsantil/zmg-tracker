namespace Zmg.Domain.Tests;

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