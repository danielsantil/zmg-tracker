namespace Zmg.Domain.Tests;

public class ReleaseArchivalTests
{
    private static readonly DateOnly Today = TestDates.Today;

    [Theory]
    [InlineData(5, false, true)]    // upcoming, active → archivable
    [InlineData(0, false, true)]    // today counts as still-to-come → archivable
    [InlineData(-1, false, false)]  // already released → not archivable
    [InlineData(5, true, false)]    // already archived → not archivable
    public void CanArchive_requires_upcoming_and_not_already_archived(int daysFromToday, bool isArchived, bool expected)
    {
        // Arrange
        var releaseDate = Today.AddDays(daysFromToday);

        // Act
        var result = ReleaseArchival.CanArchive(releaseDate, Today, isArchived);

        // Assert
        Assert.Equal(expected, result);
    }
}
