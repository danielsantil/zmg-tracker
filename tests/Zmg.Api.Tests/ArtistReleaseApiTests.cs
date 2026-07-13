using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

public class ArtistReleaseApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private async Task<ArtistDto> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!;
    }

    [Fact]
    public async Task Health_endpoint_is_ok()
    {
        var client = factory.CreateClient();
        var res = await client.GetAsync("/api/health");
        res.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Artist_crud_roundtrip()
    {
        var client = factory.CreateClient();

        var created = await CreateArtist(client, "Roundtrip Artist");
        Assert.NotEqual(Guid.Empty, created.Id);

        var update = await client.PutAsJsonAsync($"/api/artists/{created.Id}",
            new ArtistInput("Roundtrip Artist Renamed", "some notes"));
        update.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<ArtistDto>>("/api/artists");
        Assert.Contains(list!, a => a.Name == "Roundtrip Artist Renamed");

        var delete = await client.DeleteAsync($"/api/artists/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Duplicate_artist_name_is_rejected()
    {
        var client = factory.CreateClient();
        await CreateArtist(client, "Unique Name");

        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput("unique name", null));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Golden_path_release_gets_the_seeded_single_checklist()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Golden Path Artist");

        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Luz", ReleaseType.Single, new DateOnly(2026, 8, 14), artist.Id, null, null, null));
        res.EnsureSuccessStatusCode();

        var created = (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!;
        var detail = created.Data;

        Assert.Equal(31, detail.TotalTasks);
        Assert.Equal(0, detail.DoneTasks);
        Assert.Equal(6, detail.Phases.Single(p => p.Phase == Phase.Pre).Total);
        Assert.Equal(18, detail.Phases.Single(p => p.Phase == Phase.Release).Total);
        Assert.Equal(7, detail.Phases.Single(p => p.Phase == Phase.Post).Total);
        Assert.Contains(detail.Phases.SelectMany(p => p.Tasks), t => t.Title == "Mix/master");
    }

    [Fact]
    public async Task Album_release_gets_the_larger_album_checklist()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Album Artist");

        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Raices", ReleaseType.Album, new DateOnly(2026, 12, 1), artist.Id, null, null, null));
        res.EnsureSuccessStatusCode();

        var created = (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!;
        Assert.Equal(41, created.Data.TotalTasks);
    }

    [Fact]
    public async Task Deleting_an_artist_with_releases_conflicts()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Has Releases");

        var relRes = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Song", ReleaseType.Single, new DateOnly(2026, 9, 1), artist.Id, null, null, null));
        relRes.EnsureSuccessStatusCode();

        var delete = await client.DeleteAsync($"/api/artists/{artist.Id}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task Past_release_date_still_creates_but_returns_a_warning()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Backfill Artist");

        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Old Song", ReleaseType.Single, new DateOnly(2020, 1, 1), artist.Id, null, null, null));
        res.EnsureSuccessStatusCode();

        var created = (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!;
        Assert.NotEmpty(created.Warnings);
        Assert.Equal(ReleaseStatus.Released, created.Data.Status);
    }

    [Fact]
    public async Task Release_requires_title_and_artist()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "", ReleaseType.Single, null, Guid.Empty, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Release_list_filters_by_type()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Filter Artist");

        await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "A Single", ReleaseType.Single, new DateOnly(2026, 8, 1), artist.Id, null, null, null));
        await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "An Album", ReleaseType.Album, new DateOnly(2026, 8, 1), artist.Id, null, null, null));

        var albums = await client.GetFromJsonAsync<List<ReleaseListItemDto>>("/api/releases?type=1");
        Assert.All(albums!, r => Assert.Equal(ReleaseType.Album, r.Type));
        Assert.Contains(albums!, r => r.Title == "An Album");
        Assert.DoesNotContain(albums!, r => r.Title == "A Single");
    }
}
