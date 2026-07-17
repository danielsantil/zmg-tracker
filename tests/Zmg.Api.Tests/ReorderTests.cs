using Zmg.Api.Services;

namespace Zmg.Api.Tests;

/// <summary>
/// Unit tests for the shared reorder mechanic (M25 task 11). Three identically-named integration tests
/// used to be the only guard; the rule is pure, so it's tested directly here and kept once at the HTTP level.
/// </summary>
public class ReorderTests
{
    private sealed class Item
    {
        public Guid Id { get; init; }
        public int Position { get; set; } = -1;
    }

    [Fact]
    public void Applies_the_given_order_as_positions()
    {
        // Arrange
        var a = new Item { Id = Guid.NewGuid() };
        var b = new Item { Id = Guid.NewGuid() };
        var c = new Item { Id = Guid.NewGuid() };

        // Act — request b, c, a
        var applied = Reorder.TryApply(
            new[] { a, b, c }, new[] { b.Id, c.Id, a.Id }, x => x.Id, (x, i) => x.Position = i);

        // Assert
        Assert.True(applied);
        Assert.Equal(2, a.Position);
        Assert.Equal(0, b.Position);
        Assert.Equal(1, c.Position);
    }

    [Fact]
    public void Rejects_a_request_missing_an_id()
    {
        // Arrange
        var a = new Item { Id = Guid.NewGuid() };
        var b = new Item { Id = Guid.NewGuid() };

        // Act — only one of the two ids listed
        var applied = Reorder.TryApply(
            new[] { a, b }, new[] { a.Id }, x => x.Id, (x, i) => x.Position = i);

        // Assert
        Assert.False(applied);
        Assert.Equal(-1, a.Position);
        Assert.Equal(-1, b.Position);
    }

    [Fact]
    public void Rejects_a_request_with_an_unknown_id()
    {
        // Arrange
        var a = new Item { Id = Guid.NewGuid() };

        // Act — right count, wrong id
        var applied = Reorder.TryApply(
            new[] { a }, new[] { Guid.NewGuid() }, x => x.Id, (x, i) => x.Position = i);

        // Assert
        Assert.False(applied);
    }
}
