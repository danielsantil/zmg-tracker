namespace Zmg.Domain.Tests;

public class ReleaseMutabilityTests
{
    [Theory]
    [InlineData(false, true)]  // active release accepts edits
    [InlineData(true, false)]  // archived release is read-only
    public void CanEdit_is_false_only_when_archived(bool isArchived, bool expected)
    {
        // Act
        var result = ReleaseMutability.CanEdit(isArchived);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ArchivedReadOnlyMessage_is_populated()
    {
        Assert.False(string.IsNullOrWhiteSpace(ReleaseMutability.ArchivedReadOnlyMessage));
    }
}
