using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>
/// v2.0 tracks: a Release↔Song join addressed by songId. Covers create-form inline tracks, the
/// add/delete/reorder/focus endpoints, single-vs-album rules, and the song-survives-as-orphan
/// behaviour on delete.
/// </summary>
public class TrackApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private async Task<ArtistDto> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!;
    }

    private async Task<ReleaseDetailDto> Create(
        HttpClient client, Guid artistId, string title, ReleaseType type, List<TrackInput>? tracks)
    {
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, type, new DateOnly(2026, 8, 14), artistId, null, null, tracks));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }

    private async Task<ReleaseDetailDto> CreateAlbum(HttpClient client, string artistName, string title)
    {
        var artist = await CreateArtist(client, artistName);
        return await Create(client, artist.Id, title, ReleaseType.Album, null);
    }

    private static async Task<TrackDto> AddTrack(HttpClient client, Guid releaseId, TrackInput input)
    {
        var res = await client.PostAsJsonAsync($"/api/releases/{releaseId}/tracks", input);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<TrackDto>())!;
    }

    private static TrackInput NewTrack(string title) => new(null, title, null, null);

    // ---- Empty-album advisory ----

    [Fact]
    public async Task Empty_album_flags_the_advisory_until_a_track_is_added()
    {
        var client = factory.CreateClient();
        var album = await CreateAlbum(client, "Empty Album Advisory Artist", "Empty Album Advisory");

        // Fresh album with no tracks → flagged on the detail and in the list.
        Assert.True(album.IsEmptyAlbum);
        var listed = (await client.GetFromJsonAsync<List<ReleaseListItemDto>>("/api/releases?scope=all"))!
            .Single(r => r.Id == album.Id);
        Assert.True(listed.IsEmptyAlbum);

        await AddTrack(client, album.Id, NewTrack("First Track"));

        var afterDetail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{album.Id}");
        Assert.False(afterDetail!.IsEmptyAlbum);
        var afterListed = (await client.GetFromJsonAsync<List<ReleaseListItemDto>>("/api/releases?scope=all"))!
            .Single(r => r.Id == album.Id);
        Assert.False(afterListed.IsEmptyAlbum);
    }

    [Fact]
    public async Task Single_is_never_flagged_as_an_empty_album()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Single Never Empty Artist");
        var single = await Create(client, artist.Id, "Solo", ReleaseType.Single,
            new List<TrackInput> { NewTrack("Only Track") });
        Assert.False(single.IsEmptyAlbum);
    }

    // ---- Create-form inline tracks ----

    [Fact]
    public async Task Single_with_one_inline_track_creates_song_with_inherited_main_artist_isrc_and_feats()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Single Track Artist");
        var feat = await CreateArtist(client, "Feature Artist");

        var detail = await Create(client, artist.Id, "Luz", ReleaseType.Single,
            new List<TrackInput>
            {
                new(null, "Luz", "US-ABC-26-00001",
                    new List<SongArtistInput> { new(feat.Id, ArtistRole.Featured) }),
            });

        var track = Assert.Single(detail.Tracks);
        Assert.Equal("Luz", track.Title);
        Assert.Equal("US-ABC-26-00001", track.Isrc);
        Assert.Equal(1, track.TrackNumber);
        var artistOnSong = Assert.Single(track.Artists);
        Assert.Equal(feat.Id, artistOnSong.ArtistId);
        Assert.Equal(ArtistRole.Featured, artistOnSong.Role);
    }

    [Fact]
    public async Task Single_with_zero_or_two_tracks_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Bad Single Artist");

        var zero = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Zero", ReleaseType.Single, new DateOnly(2026, 8, 14), artist.Id, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);

        var two = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Two", ReleaseType.Single, new DateOnly(2026, 8, 14), artist.Id, null, null,
            new List<TrackInput> { NewTrack("A"), NewTrack("B") }));
        Assert.Equal(HttpStatusCode.BadRequest, two.StatusCode);
    }

    [Fact]
    public async Task Album_can_be_created_with_zero_tracks()
    {
        var client = factory.CreateClient();
        var album = await CreateAlbum(client, "Empty Album Artist", "Empty Album");
        Assert.Empty(album.Tracks);
    }

    [Fact]
    public async Task Duplicate_song_title_for_same_artist_warns_but_creates()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Dup Title Artist");

        // First release establishes a song titled "Echo".
        await Create(client, artist.Id, "Echo Single", ReleaseType.Single,
            new List<TrackInput> { NewTrack("Echo") });

        // A new-title track re-using that title for the same artist → 201 with warning.
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Echo Album", ReleaseType.Album, new DateOnly(2026, 8, 14), artist.Id, null, null,
            new List<TrackInput> { NewTrack("echo") }));
        res.EnsureSuccessStatusCode();
        var created = (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!;
        Assert.Contains(created.Warnings, w => w.Contains("already exists"));
    }

    // ---- Add / delete / reorder / focus ----

    [Fact]
    public async Task Add_track_appends_with_next_number_and_shows_in_detail()
    {
        var client = factory.CreateClient();
        var album = await CreateAlbum(client, "Add Track Artist", "Add Track Album");

        var t1 = await AddTrack(client, album.Id, NewTrack("Intro"));
        var t2 = await AddTrack(client, album.Id, NewTrack("Focus Song"));
        Assert.Equal(1, t1.TrackNumber);
        Assert.Equal(2, t2.TrackNumber);
        Assert.False(t1.IsFocusTrack);

        var detail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{album.Id}");
        Assert.Equal(new[] { "Intro", "Focus Song" }, detail!.Tracks.Select(t => t.Title).ToArray());
    }

    [Fact]
    public async Task Add_track_with_neither_song_nor_title_is_rejected()
    {
        var client = factory.CreateClient();
        var album = await CreateAlbum(client, "Blank Track Artist", "Blank Track Album");

        var res = await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks",
            new TrackInput(null, "   ", null, null));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Add_and_delete_track_on_single_conflict()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Single Conflict Artist");
        var single = await Create(client, artist.Id, "Solo", ReleaseType.Single,
            new List<TrackInput> { NewTrack("Solo") });

        var add = await client.PostAsJsonAsync($"/api/releases/{single.Id}/tracks", NewTrack("Extra"));
        Assert.Equal(HttpStatusCode.Conflict, add.StatusCode);

        var songId = single.Tracks.Single().SongId;
        var del = await client.DeleteAsync($"/api/releases/{single.Id}/tracks/{songId}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
    }

    [Fact]
    public async Task Same_song_twice_on_album_conflicts()
    {
        var client = factory.CreateClient();
        var album = await CreateAlbum(client, "Same Song Artist", "Same Song Album");

        var track = await AddTrack(client, album.Id, NewTrack("Once"));
        var again = await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks",
            new TrackInput(track.SongId, null, null, null));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Delete_album_track_removes_join_and_song_survives_as_orphan()
    {
        var client = factory.CreateClient();
        var album = await CreateAlbum(client, "Delete Track Artist", "Delete Track Album");
        var a = await AddTrack(client, album.Id, NewTrack("A"));
        var b = await AddTrack(client, album.Id, NewTrack("B"));
        var c = await AddTrack(client, album.Id, NewTrack("C"));

        var del = await client.DeleteAsync($"/api/releases/{album.Id}/tracks/{b.SongId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var detail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{album.Id}");
        var after = detail!.Tracks.OrderBy(t => t.TrackNumber).ToList();
        Assert.Equal(new[] { "A", "C" }, after.Select(t => t.Title).ToArray());
        Assert.Equal(new[] { 1, 2 }, after.Select(t => t.TrackNumber).ToArray());
        Assert.DoesNotContain(after, t => t.SongId == b.SongId);

        // The song survived the delete: re-linking it to the album by its id succeeds.
        var relink = await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks",
            new TrackInput(b.SongId, null, null, null));
        Assert.Equal(HttpStatusCode.Created, relink.StatusCode);
        _ = (a, c);
    }

    [Fact]
    public async Task Reorder_by_song_ids_reverses_and_renumbers_contiguously()
    {
        var client = factory.CreateClient();
        var album = await CreateAlbum(client, "Reorder Track Artist", "Reorder Track Album");
        var a = await AddTrack(client, album.Id, NewTrack("A"));
        var b = await AddTrack(client, album.Id, NewTrack("B"));
        var c = await AddTrack(client, album.Id, NewTrack("C"));

        var reversed = new List<Guid> { c.SongId, b.SongId, a.SongId };
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
        var client = factory.CreateClient();
        var album = await CreateAlbum(client, "Bad Reorder Track Artist", "Bad Reorder Track Album");
        var a = await AddTrack(client, album.Id, NewTrack("A"));
        await AddTrack(client, album.Id, NewTrack("B"));

        var res = await client.PutAsJsonAsync($"/api/releases/{album.Id}/tracks/order",
            new ReorderTracksInput(new List<Guid> { a.SongId }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Toggle_focus_flips_the_flag()
    {
        var client = factory.CreateClient();
        var album = await CreateAlbum(client, "Focus Toggle Artist", "Focus Toggle Album");
        var track = await AddTrack(client, album.Id, NewTrack("Single Off Album"));

        var on = await client.PatchAsync($"/api/releases/{album.Id}/tracks/{track.SongId}/focus", null);
        on.EnsureSuccessStatusCode();
        Assert.True((await on.Content.ReadFromJsonAsync<TrackDto>())!.IsFocusTrack);

        var off = await client.PatchAsync($"/api/releases/{album.Id}/tracks/{track.SongId}/focus", null);
        Assert.False((await off.Content.ReadFromJsonAsync<TrackDto>())!.IsFocusTrack);
    }

    [Fact]
    public async Task Focus_toggle_on_missing_track_is_not_found()
    {
        var client = factory.CreateClient();
        var album = await CreateAlbum(client, "Missing Focus Artist", "Missing Focus Album");
        var res = await client.PatchAsync($"/api/releases/{album.Id}/tracks/{Guid.NewGuid()}/focus", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ---- Update guard ----

    [Fact]
    public async Task Release_type_cannot_change_on_put()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Type Change Artist");
        var single = await Create(client, artist.Id, "Fixed", ReleaseType.Single,
            new List<TrackInput> { NewTrack("Fixed") });

        var res = await client.PutAsJsonAsync($"/api/releases/{single.Id}", new ReleaseInput(
            "Fixed", ReleaseType.Album, new DateOnly(2026, 8, 14), artist.Id, null, null, null));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }
}
