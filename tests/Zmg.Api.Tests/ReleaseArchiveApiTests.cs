using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>
/// M11 — the archive lifecycle: archiving a future release, the archive/remove guards, the
/// archived scope, and that archived releases contribute no pending actions. Shares one host
/// (IClassFixture): every assertion is scoped to its own release id and each test uses a uniquely-named
/// artist, so the shared seeded DB never collides (M25 — was 8 host boots).
/// </summary>
public class ReleaseArchiveApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<Guid> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!.Id;
    }

    private async Task<Guid> CreateRelease(HttpClient client, Guid artistId, string title, DateOnly date)
    {
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, date, artistId, null, null,
            new List<TrackInput> { new(null, "Track 1", null, null) }));
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>();
        return created!.Data.Id;
    }

    private static Task<List<ReleaseListItemDto>?> List(HttpClient client, string scope) =>
        client.GetFromJsonAsync<List<ReleaseListItemDto>>($"/api/releases?scope={scope}");

    [Fact]
    public async Task Archiving_a_future_release_moves_it_to_the_archived_scope()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Archive Artist");
        var id = await CreateRelease(client, artist, "Upcoming Single", Today.AddDays(30));

        var res = await client.PostAsync($"/api/releases/{id}/archive", null);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        Assert.DoesNotContain((await List(client, "home"))!, r => r.Id == id);
        Assert.DoesNotContain((await List(client, "all"))!, r => r.Id == id);
        var archived = (await List(client, "archived"))!.SingleOrDefault(r => r.Id == id);
        Assert.NotNull(archived);
        Assert.Equal(ReleaseStatus.Archived, archived!.Status);

        // The detail flags it archived too.
        var detail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{id}");
        Assert.True(detail!.IsArchived);
        Assert.Equal(ReleaseStatus.Archived, detail.Status);
    }

    [Fact]
    public async Task Archiving_a_past_release_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Past Artist");
        var id = await CreateRelease(client, artist, "Already Out", Today.AddDays(-5));

        var res = await client.PostAsync($"/api/releases/{id}/archive", null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Archiving_twice_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Twice Artist");
        var id = await CreateRelease(client, artist, "Once", Today.AddDays(10));

        (await client.PostAsync($"/api/releases/{id}/archive", null)).EnsureSuccessStatusCode();
        var again = await client.PostAsync($"/api/releases/{id}/archive", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Remove_hard_deletes_an_archived_release()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Remove Artist");
        var id = await CreateRelease(client, artist, "To Remove", Today.AddDays(10));

        (await client.PostAsync($"/api/releases/{id}/archive", null)).EnsureSuccessStatusCode();
        var del = await client.DeleteAsync($"/api/releases/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Row is gone (M36 hard-delete): missing from every scope, and the detail 404s.
        Assert.DoesNotContain((await List(client, "archived"))!, r => r.Id == id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/releases/{id}")).StatusCode);
    }

    [Fact]
    public async Task Remove_on_an_active_release_is_rejected()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Active Artist");
        var id = await CreateRelease(client, artist, "Still Active", Today.AddDays(10));

        var del = await client.DeleteAsync($"/api/releases/{id}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
    }

    [Fact]
    public async Task Archive_preview_lists_an_exclusive_upcoming_song()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Preview Artist");
        var id = await CreateRelease(client, artist, "Preview Single", Today.AddDays(30));

        var preview = await client.GetFromJsonAsync<ArchivePreviewDto>($"/api/releases/{id}/archive-preview");
        Assert.Equal(new[] { "Track 1" }, preview!.SongsToArchive);
    }

    [Fact]
    public async Task Archive_preview_excludes_a_previously_released_song()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Shared Preview Artist");

        // A future single carries a song…
        var single = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Future Single", ReleaseType.Single, Today.AddDays(30), artist, null, null,
            new List<TrackInput> { new(null, "Shared Song", null, null) }));
        var songId = (await single.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!
            .Data.Tracks.Single().SongId;

        // …that already came out on a past-dated album → the song is "released", so it must not cascade.
        var album = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Past Album", ReleaseType.Album, Today.AddDays(-10), artist, null, null, null));
        var albumId = (await album.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data.Id;
        (await client.PostAsJsonAsync($"/api/releases/{albumId}/tracks",
            new TrackInput(songId, null, null, null))).EnsureSuccessStatusCode();

        var singleId = (await List(client, "all"))!.Single(r => r.Title == "Future Single").Id;
        var preview = await client.GetFromJsonAsync<ArchivePreviewDto>($"/api/releases/{singleId}/archive-preview");
        Assert.Empty(preview!.SongsToArchive);
    }

    [Fact]
    public async Task Archived_release_contributes_no_pending_actions()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Pending Artist");
        // A release 3 days out is inside the Distribute/Pitch window, so it would normally pend.
        var id = await CreateRelease(client, artist, "Soon", Today.AddDays(3));

        var before = await client.GetFromJsonAsync<List<PendingAction>>("/api/pending");
        Assert.Contains(before!, p => p.ReleaseId == id);

        (await client.PostAsync($"/api/releases/{id}/archive", null)).EnsureSuccessStatusCode();

        var after = await client.GetFromJsonAsync<List<PendingAction>>("/api/pending");
        Assert.DoesNotContain(after!, p => p.ReleaseId == id);
    }
}
