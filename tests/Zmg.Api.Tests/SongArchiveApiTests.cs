using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>
/// M15 — the song archive lifecycle: the release-archive cascade, manual archive/delete guards,
/// the archived scope + read-only PUT, and that archived songs contribute no pending action.
/// Shares one host (IClassFixture): every test scopes its assertions to its own songId/releaseId and
/// creates a uniquely-named artist, so the shared seeded DB never collides (M25 — was 13 host boots).
/// </summary>
public class SongArchiveApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static async Task<Guid> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!.Id;
    }

    private static async Task<ReleaseDetailDto> CreateSingle(
        HttpClient client, Guid artistId, string title, DateOnly date)
    {
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, date, artistId, null, null,
            new List<TrackInput> { new(null, $"{title} Song", null, null) }));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }

    private static async Task<ReleaseDetailDto> CreateAlbum(
        HttpClient client, Guid artistId, string title, DateOnly date, List<TrackInput>? tracks = null)
    {
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Album, date, artistId, null, null, tracks));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }

    private static Task<List<SongListItemDto>?> ListSongs(HttpClient client, string scope = "all") =>
        client.GetFromJsonAsync<List<SongListItemDto>>($"/api/songs?scope={scope}");

    private static async Task<SongListItemDto> FindSong(HttpClient client, Guid songId, string scope = "all") =>
        (await ListSongs(client, scope))!.Single(s => s.Id == songId);

    // ---- Release-archive cascade ----
    // The exclude-shared and exclude-released rule variations are pure and covered by SongArchivalTests
    // (Song_shared_with_another_active_release_is_untouched / Released_song_is_untouched, M25); this keeps
    // only the positive case that proves the archive endpoint actually invokes the cascade.

    [Fact]
    public async Task Archiving_a_release_cascades_to_its_exclusive_upcoming_song()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Cascade Artist");

        var single = await CreateSingle(client, artist, "Cascade Single", Today.AddDays(30));
        var songId = single.Tracks.Single().SongId;

        (await client.PostAsync($"/api/releases/{single.Id}/archive", null)).EnsureSuccessStatusCode();

        // The song moved to the archived scope with the release.
        Assert.DoesNotContain((await ListSongs(client))!, s => s.Id == songId);
        Assert.Contains((await ListSongs(client, "archived"))!, s => s.Id == songId);
        var detail = await client.GetFromJsonAsync<SongDetailDto>($"/api/songs/{songId}");
        Assert.True(detail!.IsArchived);
    }

    // ---- Manual archive guards ----

    [Fact]
    public async Task Manual_archive_of_an_orphan_song_succeeds()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Orphan Archive Artist");

        // Create then unlink to make an orphan.
        var album = await CreateAlbum(client, artist, "Temp Album", Today.AddDays(30),
            new List<TrackInput> { new(null, "Soon Orphan", null, null) });
        var songId = album.Tracks.Single().SongId;
        (await client.DeleteAsync($"/api/releases/{album.Id}/tracks/{songId}")).EnsureSuccessStatusCode();

        var orphan = await FindSong(client, songId);
        Assert.Null(orphan.ReleaseDate);       // orphan → archivable (M38: ReleaseDate == null)
        Assert.Equal(0, orphan.ReleaseCount);

        var res = await client.PostAsync($"/api/songs/{songId}/archive", null);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task Manual_archive_of_a_song_on_an_active_release_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Active Song Artist");

        var single = await CreateSingle(client, artist, "Active Single", Today.AddDays(30));
        var songId = single.Tracks.Single().SongId;

        var res = await client.PostAsync($"/api/songs/{songId}/archive", null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Manual_archive_of_a_released_song_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Released Song Artist");

        var single = await CreateSingle(client, artist, "Out Single", Today.AddDays(-3));
        var songId = single.Tracks.Single().SongId;

        var res = await client.PostAsync($"/api/songs/{songId}/archive", null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Manual_archive_of_unknown_song_is_not_found()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsync($"/api/songs/{Guid.NewGuid()}/archive", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ---- Delete guards ----

    [Fact]
    public async Task Delete_an_archived_song_succeeds()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Delete Archived Artist");

        var single = await CreateSingle(client, artist, "Delete Single", Today.AddDays(30));
        var songId = single.Tracks.Single().SongId;
        (await client.PostAsync($"/api/releases/{single.Id}/archive", null)).EnsureSuccessStatusCode();

        var del = await client.DeleteAsync($"/api/songs/{songId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/songs/{songId}")).StatusCode);
    }

    [Fact]
    public async Task Delete_an_orphan_song_succeeds_without_archiving_first()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Delete Orphan Artist");

        var album = await CreateAlbum(client, artist, "Orphan Source", Today.AddDays(30),
            new List<TrackInput> { new(null, "Doomed", null, null) });
        var songId = album.Tracks.Single().SongId;
        (await client.DeleteAsync($"/api/releases/{album.Id}/tracks/{songId}")).EnsureSuccessStatusCode();

        var del = await client.DeleteAsync($"/api/songs/{songId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task Delete_a_song_on_an_active_release_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Delete Active Artist");

        var single = await CreateSingle(client, artist, "Keep Single", Today.AddDays(30));
        var songId = single.Tracks.Single().SongId;

        var del = await client.DeleteAsync($"/api/songs/{songId}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
    }

    // ---- Read-only when archived ----

    [Fact]
    public async Task Updating_an_archived_song_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "RO Artist");

        var single = await CreateSingle(client, artist, "RO Single", Today.AddDays(30));
        var songId = single.Tracks.Single().SongId;
        (await client.PostAsync($"/api/releases/{single.Id}/archive", null)).EnsureSuccessStatusCode();

        var res = await client.PutAsJsonAsync($"/api/songs/{songId}",
            new SongUpdateInput("New Name", artist, null, null));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Adding_an_archived_song_to_a_release_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Add Archived Artist");

        var single = await CreateSingle(client, artist, "Archived Source", Today.AddDays(30));
        var songId = single.Tracks.Single().SongId;
        (await client.PostAsync($"/api/releases/{single.Id}/archive", null)).EnsureSuccessStatusCode();

        var album = await CreateAlbum(client, artist, "Fresh Album", Today.AddDays(40));
        var res = await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks",
            new TrackInput(songId, null, null, null));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Archived_song_contributes_no_pending_action()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Archived Pending Artist");

        // Upcoming single, distributed by hand → its song pends for a missing ISRC while active.
        var single = await CreateSingle(client, artist, "Upcoming Distributed", Today.AddDays(30));
        var songId = single.Tracks.Single().SongId;
        var distribute = single.Phases.SelectMany(p => p.Tasks)
            .Single(t => t.Title == SeedData.DistributeToDspsTitle);
        (await client.PatchAsync($"/api/tasks/{distribute.Id}/toggle", null)).EnsureSuccessStatusCode();

        Assert.Contains((await client.GetFromJsonAsync<List<PendingAction>>("/api/pending"))!,
            p => p.SongId == songId && p.Kind == PendingKind.MissingIsrc);

        // Archiving the (still upcoming, exclusive) release cascades the song into the archive.
        (await client.PostAsync($"/api/releases/{single.Id}/archive", null)).EnsureSuccessStatusCode();

        var after = (await client.GetFromJsonAsync<List<PendingAction>>("/api/pending"))!;
        Assert.DoesNotContain(after, p => p.SongId == songId);
    }
}
