using Zmg.Domain;

namespace Zmg.Domain.Tests;

/// <summary>M15 — the pure release-archive cascade rule (which songs follow a release into the archive).</summary>
public class SongArchivalTests
{
    private static readonly DateOnly Today = TestDates.Today;

    [Fact]
    public void Exclusive_upcoming_song_cascades()
    {
        // Arrange
        var archiving = Builders.Release(Today.AddDays(30));
        var song = Builders.SongOn(archiving);

        // Act
        var shouldArchive = SongArchival.ShouldArchive(song, archiving.Id, Today);

        // Assert
        Assert.True(shouldArchive);
    }

    [Fact]
    public void Released_song_is_untouched()
    {
        // Arrange — the song also sits on a past-dated release → it's out; never cascade.
        var archiving = Builders.Release(Today.AddDays(30));
        var past = Builders.Release(Today.AddDays(-1));
        var song = Builders.SongOn(archiving, past);

        // Act
        var shouldArchive = SongArchival.ShouldArchive(song, archiving.Id, Today);

        // Assert
        Assert.False(shouldArchive);
    }

    [Fact]
    public void Song_shared_with_another_active_release_is_untouched()
    {
        // Arrange
        var archiving = Builders.Release(Today.AddDays(30));
        var otherActive = Builders.Release(Today.AddDays(40)); // upcoming, not archived
        var song = Builders.SongOn(archiving, otherActive);

        // Act
        var shouldArchive = SongArchival.ShouldArchive(song, archiving.Id, Today);

        // Assert
        Assert.False(shouldArchive);
    }

    [Fact]
    public void Song_shared_only_with_already_archived_releases_cascades()
    {
        // Arrange
        var archiving = Builders.Release(Today.AddDays(30));
        var otherArchived = Builders.Release(Today.AddDays(40), archived: true);
        var song = Builders.SongOn(archiving, otherArchived);

        // Act
        var shouldArchive = SongArchival.ShouldArchive(song, archiving.Id, Today);

        // Assert
        Assert.True(shouldArchive);
    }

    [Fact]
    public void Already_archived_song_does_not_cascade_again()
    {
        // Arrange
        var archiving = Builders.Release(Today.AddDays(30));
        var song = Builders.SongOn(archiving);
        song.ArchivedAt = DateTime.UtcNow;

        // Act
        var shouldArchive = SongArchival.ShouldArchive(song, archiving.Id, Today);

        // Assert
        Assert.False(shouldArchive);
    }

    [Fact]
    public void Works_whether_or_not_the_archiving_release_is_already_stamped()
    {
        // Arrange — call order shouldn't matter: the archiving release is treated as archived either way.
        var archiving = Builders.Release(Today.AddDays(30), archived: true);
        var song = Builders.SongOn(archiving);

        // Act
        var shouldArchive = SongArchival.ShouldArchive(song, archiving.Id, Today);

        // Assert
        Assert.True(shouldArchive);
    }
}
