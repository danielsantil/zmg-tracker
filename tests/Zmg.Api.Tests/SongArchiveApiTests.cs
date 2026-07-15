using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>
/// M15 — the song archive lifecycle: the release-archive cascade, manual archive/delete guards,
/// the archived scope + read-only PUT, and that archived songs contribute no pending action.
/// </summary>
public class SongArchiveApiTests
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

    [Fact]
    public async Task Archiving_a_release_cascades_to_its_exclusive_upcoming_song()
    {
        using var factory = new ZmgApiFactory();
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

    [Fact]
    public async Task Archiving_a_release_leaves_a_song_shared_with_an_active_release_untouched()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Shared Cascade Artist");

        // One song on two upcoming albums; archive only the first.
        var albumA = await CreateAlbum(client, artist, "Album A", Today.AddDays(20),
            new List<TrackInput> { new(null, "Shared", null, null), new(null, "Filler A", null, null) });
        var songId = albumA.Tracks.First(t => t.Title == "Shared").SongId;

        var albumB = await CreateAlbum(client, artist, "Album B", Today.AddDays(40),
            new List<TrackInput> { new(songId, null, null, null), new(null, "Filler B", null, null) });

        (await client.PostAsync($"/api/releases/{albumA.Id}/archive", null)).EnsureSuccessStatusCode();

        // The shared song stays active because Album B is still active.
        Assert.Contains((await ListSongs(client))!, s => s.Id == songId);
        _ = albumB;
    }

    [Fact]
    public async Task Archiving_a_release_leaves_a_released_song_untouched()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Released Cascade Artist");

        // Song first appeared on a past single (released), then re-used on an upcoming album.
        var past = await CreateSingle(client, artist, "Past Single", Today.AddDays(-5));
        var songId = past.Tracks.Single().SongId;
        var album = await CreateAlbum(client, artist, "Reissue Album", Today.AddDays(30),
            new List<TrackInput> { new(songId, null, null, null), new(null, "New Track", null, null) });

        (await client.PostAsync($"/api/releases/{album.Id}/archive", null)).EnsureSuccessStatusCode();

        // Released → never cascades.
        Assert.Contains((await ListSongs(client))!, s => s.Id == songId);
    }

    // ---- Manual archive guards ----

    [Fact]
    public async Task Manual_archive_of_an_orphan_song_succeeds()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Orphan Archive Artist");

        // Create then unlink to make an orphan.
        var album = await CreateAlbum(client, artist, "Temp Album", Today.AddDays(30),
            new List<TrackInput> { new(null, "Soon Orphan", null, null) });
        var songId = album.Tracks.Single().SongId;
        (await client.DeleteAsync($"/api/releases/{album.Id}/tracks/{songId}")).EnsureSuccessStatusCode();

        var orphan = await FindSong(client, songId);
        Assert.True(orphan.IsOrphan);

        var res = await client.PostAsync($"/api/songs/{songId}/archive", null);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task Manual_archive_of_a_song_on_an_active_release_is_rejected()
    {
        using var factory = new ZmgApiFactory();
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
        using var factory = new ZmgApiFactory();
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
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var res = await client.PostAsync($"/api/songs/{Guid.NewGuid()}/archive", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ---- Delete guards ----

    [Fact]
    public async Task Delete_an_archived_song_succeeds()
    {
        using var factory = new ZmgApiFactory();
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
        using var factory = new ZmgApiFactory();
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
        using var factory = new ZmgApiFactory();
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
        using var factory = new ZmgApiFactory();
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
        using var factory = new ZmgApiFactory();
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
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Archived Pending Artist");

        // Upcoming single, distributed by hand → its song pends for a missing ISRC while active.
        var single = await CreateSingle(client, artist, "Upcoming Distributed", Today.AddDays(30));
        var songId = single.Tracks.Single().SongId;
        var distribute = single.Phases.SelectMany(p => p.Tasks)
            .Single(t => t.Title == Zmg.Domain.SeedData.DistributeToDspsTitle);
        (await client.PatchAsync($"/api/tasks/{distribute.Id}/toggle", null)).EnsureSuccessStatusCode();

        Assert.Contains((await client.GetFromJsonAsync<List<Zmg.Domain.PendingAction>>("/api/pending"))!,
            p => p.SongId == songId && p.Kind == PendingKind.MissingIsrc);

        // Archiving the (still upcoming, exclusive) release cascades the song into the archive.
        (await client.PostAsync($"/api/releases/{single.Id}/archive", null)).EnsureSuccessStatusCode();

        var after = (await client.GetFromJsonAsync<List<Zmg.Domain.PendingAction>>("/api/pending"))!;
        Assert.DoesNotContain(after, p => p.SongId == songId);
    }
}
