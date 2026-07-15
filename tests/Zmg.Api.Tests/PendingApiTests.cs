using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>
/// M10/M14 — the aggregate GET /api/pending ordering across releases and songs, the missing-UPC /
/// missing-ISRC / empty-album kinds, and the per-release GET /api/pending/{id} scoping (incl. its
/// rolled-up "tracks missing ISRC" view).
/// </summary>
public class PendingApiTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static async Task<Guid> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!.Id;
    }

    private static async Task<ReleaseDetailDto> CreateSingle(
        HttpClient client, Guid artistId, string title, DateOnly date, string? isrc = null)
    {
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, date, artistId, null, null,
            new List<TrackInput> { new(null, "Track 1", isrc, null) }));
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

    private static Task<List<PendingAction>?> GetPending(HttpClient client) =>
        client.GetFromJsonAsync<List<PendingAction>>("/api/pending");

    // ---- Aggregate ordering & kinds ----

    [Fact]
    public async Task Pending_orders_task_due_nearest_first_then_data_items_last()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Pending Artist");

        // Two forward releases inside the seeded 7–14 day windows, and one past release (auto-distributed
        // with a blank UPC → a missing-UPC data action; its song's blank ISRC → a missing-ISRC data action).
        await CreateSingle(client, artist, "Near", Today.AddDays(3));
        await CreateSingle(client, artist, "Far", Today.AddDays(10));
        await CreateSingle(client, artist, "Past", Today.AddDays(-5));

        var pending = await GetPending(client);
        Assert.NotNull(pending);

        var taskDue = pending.Where(a => a.Kind == PendingKind.TaskDue).ToList();

        // Every task-due item comes before every data item.
        var lastTaskDueIdx = pending.FindLastIndex(a => a.Kind == PendingKind.TaskDue);
        var firstDataIdx = pending.FindIndex(a => a.Kind != PendingKind.TaskDue);
        Assert.True(firstDataIdx > lastTaskDueIdx);

        // Task-due items are ordered nearest-release-first (non-decreasing days-to-release).
        for (var i = 1; i < taskDue.Count; i++)
            Assert.True(taskDue[i - 1].DaysToRelease <= taskDue[i].DaysToRelease);
        Assert.Equal("Near", taskDue.First().Subject);

        // The past release surfaces both a missing-UPC (release-owned) and a missing-ISRC (song-owned) action.
        Assert.Contains(pending, a => a.Kind == PendingKind.MissingUpc && a.Subject == "Past");
        Assert.Contains(pending, a => a.Kind == PendingKind.MissingIsrc && a.ReleaseId == null && a.SongId != null);
    }

    [Fact]
    public async Task Undistributed_future_song_has_no_isrc_action()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Future Artist");

        // Forward single, never distributed, blank ISRC → no missing-ISRC action.
        await CreateSingle(client, artist, "Future", Today.AddDays(30));

        var pending = await GetPending(client);
        Assert.DoesNotContain(pending!, a => a.Kind == PendingKind.MissingIsrc);
    }

    [Fact]
    public async Task Song_provided_with_isrc_has_no_isrc_action_even_when_distributed()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Isrc Artist");

        // Past (auto-distributed) single whose track carries an ISRC → no missing-ISRC action.
        await CreateSingle(client, artist, "HasIsrc", Today.AddDays(-3), isrc: "US-ABC-26-1");

        var pending = await GetPending(client);
        Assert.DoesNotContain(pending!, a => a.Kind == PendingKind.MissingIsrc);
    }

    [Fact]
    public async Task Song_on_two_distributed_releases_yields_exactly_one_isrc_action()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Shared Song Artist");

        // A distributed single, then the same song linked onto a distributed album.
        var single = await CreateSingle(client, artist, "Shared Single", Today.AddDays(-10));
        var songId = single.Tracks.Single().SongId;

        var album = await CreateAlbum(client, artist, "Shared Album", Today.AddDays(-5));
        (await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks",
            new TrackInput(songId, null, null, null))).EnsureSuccessStatusCode();

        var pending = await GetPending(client);
        var isrcActions = pending!.Where(a => a.Kind == PendingKind.MissingIsrc && a.SongId == songId).ToList();
        Assert.Single(isrcActions);
    }

    [Fact]
    public async Task Empty_album_actions_track_the_tracklist_count()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Album Artist");

        // 0 tracks → "Album is empty".
        var album = await CreateAlbum(client, artist, "Growing Album", Today.AddDays(30));
        var emptyAction = Assert.Single((await GetPending(client))!, a => a.Kind == PendingKind.EmptyAlbum);
        Assert.Equal("Album is empty", emptyAction.Label);
        Assert.Equal(album.Id, emptyAction.ReleaseId);

        // 1 track → "Album has only 1 track".
        (await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks",
            new TrackInput(null, "One", null, null))).EnsureSuccessStatusCode();
        var oneAction = Assert.Single((await GetPending(client))!, a => a.Kind == PendingKind.EmptyAlbum);
        Assert.Equal("Album has only 1 track", oneAction.Label);

        // 2 tracks → no empty-album action.
        (await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks",
            new TrackInput(null, "Two", null, null))).EnsureSuccessStatusCode();
        Assert.DoesNotContain((await GetPending(client))!, a => a.Kind == PendingKind.EmptyAlbum);
    }

    [Fact]
    public async Task Archived_album_has_no_empty_album_action()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Archived Album Artist");

        // Empty album with a future date so it can be archived (archive guards releaseDate >= today).
        var album = await CreateAlbum(client, artist, "Archived Album", Today.AddDays(30));
        Assert.Contains((await GetPending(client))!, a => a.Kind == PendingKind.EmptyAlbum && a.ReleaseId == album.Id);

        (await client.PostAsync($"/api/releases/{album.Id}/archive", null)).EnsureSuccessStatusCode();
        Assert.DoesNotContain((await GetPending(client))!, a => a.ReleaseId == album.Id);
    }

    // ---- Per-release scoping ----

    [Fact]
    public async Task Pending_by_release_returns_only_that_releases_actions_and_songs()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "By Release Artist");

        // A distributed past single: its own missing-UPC plus the rolled-up missing-ISRC for its song.
        var single = await CreateSingle(client, artist, "Scoped", Today.AddDays(-5));
        var songId = single.Tracks.Single().SongId;
        // Another release whose actions must not leak into the per-release result.
        await CreateSingle(client, artist, "Other", Today.AddDays(-5));

        var pending = await client.GetFromJsonAsync<List<PendingAction>>($"/api/pending/{single.Id}");
        Assert.NotNull(pending);
        Assert.Contains(pending, a => a.Kind == PendingKind.MissingUpc && a.ReleaseId == single.Id);

        var isrc = Assert.Single(pending, a => a.Kind == PendingKind.MissingIsrc);
        Assert.Equal(songId, isrc.SongId);
    }

    [Fact]
    public async Task Pending_by_release_is_empty_for_unknown_release()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();

        var pending = await client.GetFromJsonAsync<List<PendingAction>>($"/api/pending/{Guid.NewGuid()}");
        Assert.NotNull(pending);
        Assert.Empty(pending);
    }
}
