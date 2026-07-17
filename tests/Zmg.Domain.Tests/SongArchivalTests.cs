using Zmg.Domain;
using Zmg.Domain.Entities;

namespace Zmg.Domain.Tests;

/// <summary>M15 — the pure release-archive cascade rule (which songs follow a release into the archive).</summary>
public class SongArchivalTests
{
    private static readonly DateOnly Today = TestDates.Today;

    private static Release Rel(Guid id, DateOnly date, bool archived = false) =>
        new() { Id = id, ReleaseDate = date, ArchivedAt = archived ? DateTime.UtcNow : null };

    // A song with links to the given releases (join rows point back at the loaded releases).
    private static Song SongOn(params Release[] releases)
    {
        var song = new Song { Id = Guid.NewGuid(), Title = "Song" };
        song.ReleaseLinks = releases
            .Select(r => new Track { ReleaseId = r.Id, Release = r, SongId = song.Id, Song = song })
            .ToList();
        return song;
    }

    [Fact]
    public void Exclusive_upcoming_song_cascades()
    {
        var archiving = Rel(Guid.NewGuid(), Today.AddDays(30));
        var song = SongOn(archiving);
        Assert.True(SongArchival.ShouldArchive(song, archiving.Id, Today));
    }

    [Fact]
    public void Released_song_is_untouched()
    {
        // The song also sits on a past-dated release → it's out; never cascade.
        var archiving = Rel(Guid.NewGuid(), Today.AddDays(30));
        var past = Rel(Guid.NewGuid(), Today.AddDays(-1));
        var song = SongOn(archiving, past);
        Assert.False(SongArchival.ShouldArchive(song, archiving.Id, Today));
    }

    [Fact]
    public void Song_shared_with_another_active_release_is_untouched()
    {
        var archiving = Rel(Guid.NewGuid(), Today.AddDays(30));
        var otherActive = Rel(Guid.NewGuid(), Today.AddDays(40)); // upcoming, not archived
        var song = SongOn(archiving, otherActive);
        Assert.False(SongArchival.ShouldArchive(song, archiving.Id, Today));
    }

    [Fact]
    public void Song_shared_only_with_already_archived_releases_cascades()
    {
        var archiving = Rel(Guid.NewGuid(), Today.AddDays(30));
        var otherArchived = Rel(Guid.NewGuid(), Today.AddDays(40), archived: true);
        var song = SongOn(archiving, otherArchived);
        Assert.True(SongArchival.ShouldArchive(song, archiving.Id, Today));
    }

    [Fact]
    public void Already_archived_song_does_not_cascade_again()
    {
        var archiving = Rel(Guid.NewGuid(), Today.AddDays(30));
        var song = SongOn(archiving);
        song.ArchivedAt = DateTime.UtcNow;
        Assert.False(SongArchival.ShouldArchive(song, archiving.Id, Today));
    }

    [Fact]
    public void Works_whether_or_not_the_archiving_release_is_already_stamped()
    {
        // Call order shouldn't matter: the archiving release is treated as archived either way.
        var archiving = Rel(Guid.NewGuid(), Today.AddDays(30), archived: true);
        var song = SongOn(archiving);
        Assert.True(SongArchival.ShouldArchive(song, archiving.Id, Today));
    }
}
