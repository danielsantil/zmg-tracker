using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>M13 — the catalog: songs list (derived release date), detail (links + UPCs), and editing.</summary>
public class SongApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private async Task<ArtistDto> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!;
    }

    private async Task<ReleaseDetailDto> CreateRelease(
        HttpClient client, Guid artistId, string title, ReleaseType type, DateOnly date,
        List<TrackInput>? tracks, string? upc = null)
    {
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, type, date, artistId, null, null, tracks, Upc: upc));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }

    private static TrackInput NewTrack(string title, string? isrc = null) => new(null, title, isrc, null);

    private async Task<List<SongListItemDto>> ListSongs(HttpClient client, string query = "") =>
        (await client.GetFromJsonAsync<List<SongListItemDto>>($"/api/songs{query}"))!;

    // ---- List ----

    [Fact]
    public async Task List_derives_earliest_release_date_across_two_releases()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Two Release Artist");

        // A single releases the song later; an album (created here with the SAME song) releases earlier.
        var single = await CreateRelease(client, artist.Id, "Late Single", ReleaseType.Single,
            new DateOnly(2026, 12, 1), new List<TrackInput> { NewTrack("Shared") });
        var songId = single.Tracks.Single().SongId;

        var album = await CreateRelease(client, artist.Id, "Early Album", ReleaseType.Album,
            new DateOnly(2026, 6, 1), null);
        var link = await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks",
            new TrackInput(songId, null, null, null));
        link.EnsureSuccessStatusCode();

        var song = (await ListSongs(client)).Single(s => s.Id == songId);
        Assert.Equal(new DateOnly(2026, 6, 1), song.ReleaseDate); // earliest of the two links
        Assert.Equal(2, song.ReleaseCount);
    }

    [Fact]
    public async Task Orphan_song_lists_with_null_release_date()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Orphan Artist");

        // Create an album with a track, then remove the link → the song becomes an orphan.
        var album = await CreateRelease(client, artist.Id, "Orphan Album", ReleaseType.Album,
            new DateOnly(2026, 9, 1), null);
        var add = await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks", NewTrack("Orphaned"));
        var track = (await add.Content.ReadFromJsonAsync<TrackDto>())!;
        (await client.DeleteAsync($"/api/releases/{album.Id}/tracks/{track.SongId}")).EnsureSuccessStatusCode();

        var song = (await ListSongs(client)).Single(s => s.Id == track.SongId);
        Assert.Null(song.ReleaseDate);
        Assert.Equal(0, song.ReleaseCount);
    }

    [Fact]
    public async Task List_filters_by_title_query()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Query Artist");
        await CreateRelease(client, artist.Id, "R1", ReleaseType.Single,
            new DateOnly(2026, 9, 1), new List<TrackInput> { NewTrack("Findable Song") });
        await CreateRelease(client, artist.Id, "R2", ReleaseType.Single,
            new DateOnly(2026, 9, 2), new List<TrackInput> { NewTrack("Other Track") });

        var results = await ListSongs(client, "?q=findable");
        Assert.Contains(results, s => s.Title == "Findable Song");
        Assert.DoesNotContain(results, s => s.Title == "Other Track");
    }

    // ---- Create (directly in the catalog) ----

    [Fact]
    public async Task Create_adds_an_orphan_song_to_the_catalog()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Direct Song Artist");
        var feat = await CreateArtist(client, "Direct Song Feat");

        var res = await client.PostAsJsonAsync("/api/songs", new SongCreateInput(
            "Direct Song", artist.Id, "US-DIR-26-1",
            new List<SongArtistInput> { new(feat.Id, ArtistRole.Featured) }));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<SongDetailDto>>())!.Data;

        Assert.Equal("Direct Song", created.Title);
        Assert.Equal("US-DIR-26-1", created.Isrc);
        Assert.Empty(created.Releases); // born an orphan — no release links
        Assert.Single(created.Artists, a => a.ArtistId == feat.Id);

        // It shows up in the catalog list with a null (derived) release date.
        var listed = (await ListSongs(client)).Single(s => s.Id == created.Id);
        Assert.Null(listed.ReleaseDate);
        Assert.True(listed.IsOrphan);
    }

    [Fact]
    public async Task Create_with_blank_title_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Blank Create Artist");
        var res = await client.PostAsJsonAsync("/api/songs",
            new SongCreateInput("   ", artist.Id, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_with_unknown_artist_is_rejected()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/songs",
            new SongCreateInput("Homeless Song", Guid.NewGuid(), null, null));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---- Detail ----

    [Fact]
    public async Task Detail_returns_both_release_links_with_upcs_for_a_song_released_twice()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Twice Released Artist");

        var single = await CreateRelease(client, artist.Id, "The Single", ReleaseType.Single,
            new DateOnly(2026, 7, 1), new List<TrackInput> { NewTrack("Hit") }, upc: "UPC-SINGLE");
        var songId = single.Tracks.Single().SongId;

        var album = await CreateRelease(client, artist.Id, "The Album", ReleaseType.Album,
            new DateOnly(2026, 10, 1), null, upc: "UPC-ALBUM");
        (await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks",
            new TrackInput(songId, null, null, null))).EnsureSuccessStatusCode();

        var detail = await client.GetFromJsonAsync<SongDetailDto>($"/api/songs/{songId}");
        Assert.Equal(2, detail!.Releases.Count);
        Assert.Contains(detail.Releases, r => r.Title == "The Single" && r.Upc == "UPC-SINGLE");
        Assert.Contains(detail.Releases, r => r.Title == "The Album" && r.Upc == "UPC-ALBUM");
    }

    [Fact]
    public async Task Detail_for_unknown_song_is_not_found()
    {
        var client = factory.CreateClient();
        var res = await client.GetAsync($"/api/songs/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ---- Update ----

    [Fact]
    public async Task Update_renames_sets_isrc_and_replaces_artists()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Editable Artist");
        var feat = await CreateArtist(client, "Editable Feat");

        var single = await CreateRelease(client, artist.Id, "Editable Single", ReleaseType.Single,
            new DateOnly(2026, 9, 1), new List<TrackInput> { NewTrack("Working Title") });
        var songId = single.Tracks.Single().SongId;

        var res = await client.PutAsJsonAsync($"/api/songs/{songId}", new SongUpdateInput(
            "Final Title", artist.Id, "US-XYZ-26-00009",
            new List<SongArtistInput> { new(feat.Id, ArtistRole.Collab) }));
        res.EnsureSuccessStatusCode();
        var updated = (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<SongDetailDto>>())!.Data;

        Assert.Equal("Final Title", updated.Title);
        Assert.Equal("US-XYZ-26-00009", updated.Isrc);
        var a = Assert.Single(updated.Artists);
        Assert.Equal(feat.Id, a.ArtistId);
        Assert.Equal(ArtistRole.Collab, a.Role);

        // The rename is reflected on the release that carries the song.
        var release = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{single.Id}");
        Assert.Equal("Final Title", release!.Tracks.Single().Title);
    }

    [Fact]
    public async Task Update_keeping_an_existing_feat_artist_succeeds()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Keep Artist");
        var feat = await CreateArtist(client, "Keep Feat");

        // Single whose one song already carries the feat.
        var single = await CreateRelease(client, artist.Id, "Keep Single", ReleaseType.Single,
            new DateOnly(2026, 9, 1),
            new List<TrackInput> { new(null, "Kept Song", null, new List<SongArtistInput> { new(feat.Id, ArtistRole.Featured) }) });
        var songId = single.Tracks.Single().SongId;

        // Edit the ISRC but KEEP the same feat artist → the replace clears + re-adds the same
        // composite (SongId, ArtistId) PK; must not conflict.
        var res = await client.PutAsJsonAsync($"/api/songs/{songId}", new SongUpdateInput(
            "Kept Song", artist.Id, "US-KEP-26-1",
            new List<SongArtistInput> { new(feat.Id, ArtistRole.Featured) }));
        res.EnsureSuccessStatusCode();
        var updated = (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<SongDetailDto>>())!.Data;

        Assert.Equal("US-KEP-26-1", updated.Isrc);
        Assert.Single(updated.Artists, a => a.ArtistId == feat.Id);
    }

    [Fact]
    public async Task Update_with_blank_title_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Blank Update Artist");
        var single = await CreateRelease(client, artist.Id, "Blank Update Single", ReleaseType.Single,
            new DateOnly(2026, 9, 1), new List<TrackInput> { NewTrack("Named") });
        var songId = single.Tracks.Single().SongId;

        var res = await client.PutAsJsonAsync($"/api/songs/{songId}",
            new SongUpdateInput("   ", artist.Id, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Update_unknown_song_is_not_found()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Unknown Update Artist");
        var res = await client.PutAsJsonAsync($"/api/songs/{Guid.NewGuid()}",
            new SongUpdateInput("X", artist.Id, null, null));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ---- Picker path ----

    [Fact]
    public async Task Adding_an_existing_catalog_song_to_an_album_reflects_the_catalog_title()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Catalog Add Artist");

        var single = await CreateRelease(client, artist.Id, "Catalog Single", ReleaseType.Single,
            new DateOnly(2026, 9, 1), new List<TrackInput> { NewTrack("Catalog Song") });
        var songId = single.Tracks.Single().SongId;

        var album = await CreateRelease(client, artist.Id, "Catalog Album", ReleaseType.Album,
            new DateOnly(2026, 11, 1), null);
        var add = await client.PostAsJsonAsync($"/api/releases/{album.Id}/tracks",
            new TrackInput(songId, null, null, null));
        add.EnsureSuccessStatusCode();
        var track = (await add.Content.ReadFromJsonAsync<TrackDto>())!;

        Assert.Equal(songId, track.SongId);
        Assert.Equal("Catalog Song", track.Title); // title comes from the catalog song, not re-entered
    }
}
