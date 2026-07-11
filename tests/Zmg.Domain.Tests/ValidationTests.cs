using Zmg.Domain;

namespace Zmg.Domain.Tests;

public class ValidationTests
{
    private static readonly DateOnly Today = new(2026, 7, 11);

    // ---- Artist name required + unique (case-insensitive) ----

    [Fact]
    public void Artist_name_required()
    {
        Assert.False(Validation.ValidateArtist("  ", Array.Empty<string>()).IsValid);
        Assert.True(Validation.ValidateArtist("Karen Santana", Array.Empty<string>()).IsValid);
    }

    [Fact]
    public void Artist_name_unique_case_insensitive()
    {
        var existing = new[] { "Karen Santana" };
        Assert.False(Validation.ValidateArtist("karen santana", existing).IsValid);
        Assert.False(Validation.ValidateArtist("  KAREN SANTANA ", existing).IsValid);
        Assert.True(Validation.ValidateArtist("Karen S", existing).IsValid);
    }

    // ---- Artist with releases can't be deleted ----

    [Fact]
    public void Artist_delete_blocked_when_it_has_releases()
    {
        Assert.False(Validation.ValidateArtistDelete(1).IsValid);
        Assert.True(Validation.ValidateArtistDelete(0).IsValid);
    }

    // ---- Release required fields ----

    [Fact]
    public void Release_requires_title_artist_and_date()
    {
        var artistId = Guid.NewGuid();

        var missingAll = Validation.ValidateRelease(
            "", Guid.Empty, mainArtistExists: false, null, Today, Array.Empty<string>());
        Assert.False(missingAll.IsValid);
        Assert.Equal(3, missingAll.Errors.Count); // title, artist, date

        var ok = Validation.ValidateRelease(
            "Luz", artistId, mainArtistExists: true, Today.AddDays(10), Today, Array.Empty<string>());
        Assert.True(ok.IsValid);
    }

    [Fact]
    public void Release_main_artist_must_exist()
    {
        var result = Validation.ValidateRelease(
            "Luz", Guid.NewGuid(), mainArtistExists: false, Today, Today, Array.Empty<string>());
        Assert.False(result.IsValid);
    }

    // ---- Release warnings (advise, don't block) ----

    [Fact]
    public void Past_release_date_warns_but_does_not_block()
    {
        var result = Validation.ValidateRelease(
            "Luz", Guid.NewGuid(), mainArtistExists: true, Today.AddDays(-5), Today, Array.Empty<string>());

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("past"));
    }

    [Fact]
    public void Duplicate_title_for_same_artist_warns_but_does_not_block()
    {
        var result = Validation.ValidateRelease(
            "Luz", Guid.NewGuid(), mainArtistExists: true, Today.AddDays(5), Today,
            new[] { "luz" });

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("already has a release"));
    }

    // ---- Task title non-empty ----

    [Fact]
    public void Task_title_required()
    {
        Assert.False(Validation.ValidateTaskTitle(" ").IsValid);
        Assert.True(Validation.ValidateTaskTitle("Pitch to Spotify").IsValid);
    }

    // ---- Template must keep at least one task ----

    [Fact]
    public void Template_must_keep_at_least_one_task()
    {
        Assert.False(Validation.ValidateTemplateTaskDelete(0).IsValid);
        Assert.True(Validation.ValidateTemplateTaskDelete(1).IsValid);
    }
}
