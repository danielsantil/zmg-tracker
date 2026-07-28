namespace Zmg.Domain.Tests;

public class EmailNormalizationTests
{
    [Theory]
    [InlineData("daniel@zionmusicgroup.com", "daniel@zionmusicgroup.com")] // already normal → unchanged
    [InlineData("Daniel@ZionMusicGroup.com", "daniel@zionmusicgroup.com")] // mixed case, both parts
    [InlineData("DANIEL@ZIONMUSICGROUP.COM", "daniel@zionmusicgroup.com")]
    [InlineData("  daniel@zionmusicgroup.com  ", "daniel@zionmusicgroup.com")] // pasted with padding
    [InlineData("\tDaniel@Zion.com\n", "daniel@zion.com")] // whitespace includes tabs/newlines
    public void Normalize_lowercases_and_trims(string input, string expected)
    {
        // Act
        var result = EmailNormalization.Normalize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_maps_absent_input_to_empty_rather_than_throwing(string? input)
    {
        // Act
        var result = EmailNormalization.Normalize(input);

        // Assert — callers normalize first and validate second; ordering those would be a trap.
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_is_idempotent()
    {
        // Arrange — the whitelist is written normalized and looked up normalized, so applying this
        // twice must not differ from applying it once, or a round-trip could stop matching.
        const string raw = "  Daniel@ZionMusicGroup.COM ";

        // Act
        var once = EmailNormalization.Normalize(raw);
        var twice = EmailNormalization.Normalize(once);

        // Assert
        Assert.Equal(once, twice);
    }

    [Fact]
    public void Normalize_makes_two_spellings_of_one_address_compare_equal()
    {
        // Arrange — the actual point of the class: a seeded row and a provider-supplied claim that
        // differ only in case must resolve to the same key.
        const string seeded = "daniel@zionmusicgroup.com";
        const string fromProvider = "Daniel@ZionMusicGroup.com";

        // Act & Assert — ordinal comparison, matching how the query compares them (v2.5 rule).
        Assert.Equal(
            EmailNormalization.Normalize(seeded),
            EmailNormalization.Normalize(fromProvider),
            StringComparer.Ordinal);
    }
}
