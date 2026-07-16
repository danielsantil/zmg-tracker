using Zmg.Domain.Enums;

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

    // ---- Song create/rename (v2.0) ----

    [Fact]
    public void Song_requires_title_and_main_artist()
    {
        Assert.False(Validation.ValidateSong("  ", Guid.NewGuid(), mainArtistExists: true, Array.Empty<string>()).IsValid);
        Assert.False(Validation.ValidateSong("Luz", Guid.Empty, mainArtistExists: false, Array.Empty<string>()).IsValid);
        Assert.True(Validation.ValidateSong("Luz", Guid.NewGuid(), mainArtistExists: true, Array.Empty<string>()).IsValid);
    }

    [Fact]
    public void Song_duplicate_title_for_same_artist_is_blocked()
    {
        var result = Validation.ValidateSong("Luz", Guid.NewGuid(), mainArtistExists: true, new[] { "luz" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    // ---- Release tracks (v2.0) ----

    [Fact]
    public void Single_must_have_exactly_one_track()
    {
        Assert.False(Validation.ValidateReleaseTracks(ReleaseType.Single, Array.Empty<TrackSpec>()).IsValid);
        Assert.False(Validation.ValidateReleaseTracks(ReleaseType.Single,
            new[] { new TrackSpec(null, "A"), new TrackSpec(null, "B") }).IsValid);
        Assert.True(Validation.ValidateReleaseTracks(ReleaseType.Single,
            new[] { new TrackSpec(null, "A") }).IsValid);
    }

    [Fact]
    public void Album_may_have_zero_tracks()
    {
        Assert.True(Validation.ValidateReleaseTracks(ReleaseType.Album, Array.Empty<TrackSpec>()).IsValid);
    }

    [Fact]
    public void Track_spec_must_set_exactly_one_of_id_or_title()
    {
        // Neither.
        Assert.False(Validation.ValidateReleaseTracks(ReleaseType.Album,
            new[] { new TrackSpec(null, "  ") }).IsValid);
        // Both.
        Assert.False(Validation.ValidateReleaseTracks(ReleaseType.Album,
            new[] { new TrackSpec(Guid.NewGuid(), "Title") }).IsValid);
    }

    [Fact]
    public void Duplicate_song_ids_are_rejected()
    {
        var id = Guid.NewGuid();
        Assert.False(Validation.ValidateReleaseTracks(ReleaseType.Album,
            new[] { new TrackSpec(id, null), new TrackSpec(id, null) }).IsValid);
    }

    // ---- Template must keep at least one task ----

    [Fact]
    public void Template_must_keep_at_least_one_task()
    {
        Assert.False(Validation.ValidateTemplateTaskDelete(0).IsValid);
        Assert.True(Validation.ValidateTemplateTaskDelete(1).IsValid);
    }
}
