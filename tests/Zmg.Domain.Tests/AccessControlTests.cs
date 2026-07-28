using Zmg.Domain.Entities;

namespace Zmg.Domain.Tests;

public class AccessControlTests
{
    private static AllowedUser User(DateTime? disabledAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Email = "partner@example.com",
        CreatedAt = TestDates.Today.ToDateTime(TimeOnly.MinValue),
        DisabledAt = disabledAt,
    };

    [Fact]
    public void A_listed_and_enabled_user_is_allowed()
    {
        Assert.True(AccessControl.IsAllowed(User()));
    }

    [Fact]
    public void An_address_that_was_never_listed_is_denied()
    {
        // A failed lookup arrives here as null — the commonest denial, and the one the login screen
        // must not distinguish from the next case.
        Assert.False(AccessControl.IsAllowed(null));
    }

    [Fact]
    public void A_disabled_user_is_denied_without_deleting_the_row()
    {
        // Arrange
        var revoked = User(disabledAt: TestDates.Today.ToDateTime(TimeOnly.MinValue));

        // Act & Assert — the row survives, so the fact they once had access is still on record.
        Assert.False(AccessControl.IsAllowed(revoked));
        Assert.NotNull(revoked.DisabledAt);
    }

    [Fact]
    public void Disabling_is_reversible()
    {
        // Arrange — the reason revocation sets a timestamp instead of deleting.
        var user = User(disabledAt: TestDates.Today.ToDateTime(TimeOnly.MinValue));
        Assert.False(AccessControl.IsAllowed(user));

        // Act
        user.DisabledAt = null;

        // Assert
        Assert.True(AccessControl.IsAllowed(user));
    }
}
