namespace Zmg.Domain.Tests;

public class ReleaseStatusTests
{
    private static readonly DateOnly Today = TestDates.Today;

    [Theory]
    // daysFromToday, done, total, isArchived → expected status
    [InlineData(30, 0, 30, false, ReleaseStatus.Upcoming)]  // future, open tasks
    [InlineData(-1, 5, 30, false, ReleaseStatus.Released)]  // past, open tasks
    [InlineData(0, 0, 30, false, ReleaseStatus.Released)]   // release day counts as released
    [InlineData(30, 30, 30, false, ReleaseStatus.Complete)] // all done, future
    [InlineData(-30, 30, 30, false, ReleaseStatus.Complete)]// all done, past
    [InlineData(30, 0, 30, true, ReleaseStatus.Archived)]   // archived overrides an upcoming
    [InlineData(-1, 30, 30, true, ReleaseStatus.Archived)]  // archived overrides a completed
    public void Derive_returns_the_expected_status(
        int daysFromToday, int done, int total, bool isArchived, string expected)
    {
        // Arrange
        var releaseDate = Today.AddDays(daysFromToday);

        // Act
        var status = ReleaseStatus.Derive(releaseDate, Today, new ProgressCount(done, total), isArchived);

        // Assert
        Assert.Equal(expected, status);
    }
}
