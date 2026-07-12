using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;

namespace Zmg.Api.Tests;

public class TrackApiTests : IClassFixture<ZmgApiFactory>
{
    private readonly ZmgApiFactory _factory;

    public TrackApiTests(ZmgApiFactory factory) => _factory = factory;

    private async Task<ReleaseDetailDto> CreateAlbum(HttpClient client, string artistName, string title)
    {
        var artistRes = await client.PostAsJsonAsync("/api/artists", new ArtistInput(artistName, null));
        artistRes.EnsureSuccessStatusCode();
        var artist = (await artistRes.Content.ReadFromJsonAsync<ArtistDto>())!;

        var relRes = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Album, new DateOnly(2026, 8, 14), artist.Id, null, null, null));
        relRes.EnsureSuccessStatusCode();
        return (await relRes.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }

    private static async Task<TrackDto> AddTrack(HttpClient client, Guid releaseId, string title)
    {
        var res = await client.PostAsJsonAsync($"/api/releases/{releaseId}/tracks", new AddTrackInput(title));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<TrackDto>())!;
    }

    [Fact]
    public async Task New_album_starts_with_no_tracks()
    {
        var client = _factory.CreateClient();
        var album = await CreateAlbum(client, "Empty Album Artist", "Empty Album");
        Assert.Empty(album.Tracks);
    }

    [Fact]
    public async Task Add_track_appends_with_next_number_and_shows_in_detail()
    {
        var client = _factory.CreateClient();
        var album = await CreateAlbum(client, "Add Track Artist", "Add Track Album");

        var t1 = await AddTrack(client, album.Id, "Intro");
        var t2 = await AddTrack(client, album.Id, "Focus Song");
        Assert.Equal(1, t1.TrackNumber);
        Assert.Equal(2, t2.TrackNumber);
        Assert.False(t1.IsFocusTrack);

        var detail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{album.Id}");
        Assert.Equal(new[] { "Intro", "Focus Song" }, detail!.Tracks.Select(t => t.Title).ToArray());
    }

    [Fact]
    public async Task Add_track_with_blank_title_is_rejected()
    {
        var client = _factory.CreateClient();
        var album = await CreateAlbum(client, "Blank Track Artist", "Blank Track Album");

        var res = await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks", new AddTrackInput("   "));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Update_track_renames_and_sets_focus()
    {
        var client = _factory.CreateClient();
        var album = await CreateAlbum(client, "Update Track Artist", "Update Track Album");
        var track = await AddTrack(client, album.Id, "Working Title");

        var res = await client.PutAsJsonAsync($"/api/tracks/{track.Id}", new UpdateTrackInput("Final Title", true));
        res.EnsureSuccessStatusCode();
        var updated = (await res.Content.ReadFromJsonAsync<TrackDto>())!;

        Assert.Equal("Final Title", updated.Title);
        Assert.True(updated.IsFocusTrack);
    }

    [Fact]
    public async Task Toggle_focus_flips_the_flag()
    {
        var client = _factory.CreateClient();
        var album = await CreateAlbum(client, "Focus Toggle Artist", "Focus Toggle Album");
        var track = await AddTrack(client, album.Id, "Single Off Album");

        var on = await client.PatchAsync($"/api/tracks/{track.Id}/focus", null);
        on.EnsureSuccessStatusCode();
        Assert.True((await on.Content.ReadFromJsonAsync<TrackDto>())!.IsFocusTrack);

        var off = await client.PatchAsync($"/api/tracks/{track.Id}/focus", null);
        Assert.False((await off.Content.ReadFromJsonAsync<TrackDto>())!.IsFocusTrack);
    }

    [Fact]
    public async Task Reorder_reverses_and_renumbers_contiguously()
    {
        var client = _factory.CreateClient();
        var album = await CreateAlbum(client, "Reorder Track Artist", "Reorder Track Album");
        var a = await AddTrack(client, album.Id, "A");
        var b = await AddTrack(client, album.Id, "B");
        var c = await AddTrack(client, album.Id, "C");

        var reversed = new List<Guid> { c.Id, b.Id, a.Id };
        var res = await client.PutAsJsonAsync($"/api/releases/{album.Id}/tracks/order",
            new ReorderTracksInput(reversed));
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var detail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{album.Id}");
        var after = detail!.Tracks.OrderBy(t => t.TrackNumber).ToList();
        Assert.Equal(new[] { "C", "B", "A" }, after.Select(t => t.Title).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, after.Select(t => t.TrackNumber).ToArray());
    }

    [Fact]
    public async Task Reorder_with_missing_ids_is_rejected()
    {
        var client = _factory.CreateClient();
        var album = await CreateAlbum(client, "Bad Reorder Track Artist", "Bad Reorder Track Album");
        var a = await AddTrack(client, album.Id, "A");
        await AddTrack(client, album.Id, "B");

        var res = await client.PutAsJsonAsync($"/api/releases/{album.Id}/tracks/order",
            new ReorderTracksInput(new List<Guid> { a.Id }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Delete_track_removes_it_and_renumbers_survivors()
    {
        var client = _factory.CreateClient();
        var album = await CreateAlbum(client, "Delete Track Artist", "Delete Track Album");
        var a = await AddTrack(client, album.Id, "A");
        var b = await AddTrack(client, album.Id, "B");
        var c = await AddTrack(client, album.Id, "C");

        var del = await client.DeleteAsync($"/api/tracks/{b.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var detail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{album.Id}");
        var after = detail!.Tracks.OrderBy(t => t.TrackNumber).ToList();
        Assert.Equal(new[] { "A", "C" }, after.Select(t => t.Title).ToArray());
        Assert.Equal(new[] { 1, 2 }, after.Select(t => t.TrackNumber).ToArray());
        Assert.DoesNotContain(after, t => t.Id == b.Id);
        _ = (a, c);
    }

    [Fact]
    public async Task Focus_toggle_on_missing_track_is_not_found()
    {
        var client = _factory.CreateClient();
        var res = await client.PatchAsync($"/api/tracks/{Guid.NewGuid()}/focus", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
