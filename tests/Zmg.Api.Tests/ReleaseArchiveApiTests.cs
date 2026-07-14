using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>
/// M11 — the archive lifecycle: archiving a future release, the archive/remove guards, the
/// archived scope, and that archived releases contribute no pending actions.
/// </summary>
public class ReleaseArchiveApiTests
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
            title, ReleaseType.Single, date, artistId, null, null, null));
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>();
        return created!.Data.Id;
    }

    private static Task<List<ReleaseListItemDto>?> List(HttpClient client, string scope) =>
        client.GetFromJsonAsync<List<ReleaseListItemDto>>($"/api/releases?scope={scope}");

    [Fact]
    public async Task Archiving_a_future_release_moves_it_to_the_archived_scope()
    {
        using var factory = new ZmgApiFactory();
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
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Past Artist");
        var id = await CreateRelease(client, artist, "Already Out", Today.AddDays(-5));

        var res = await client.PostAsync($"/api/releases/{id}/archive", null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Archiving_twice_is_rejected()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Twice Artist");
        var id = await CreateRelease(client, artist, "Once", Today.AddDays(10));

        (await client.PostAsync($"/api/releases/{id}/archive", null)).EnsureSuccessStatusCode();
        var again = await client.PostAsync($"/api/releases/{id}/archive", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Remove_soft_deletes_an_archived_release()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Remove Artist");
        var id = await CreateRelease(client, artist, "To Remove", Today.AddDays(10));

        (await client.PostAsync($"/api/releases/{id}/archive", null)).EnsureSuccessStatusCode();
        var del = await client.DeleteAsync($"/api/releases/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Gone from every scope, and the detail 404s.
        Assert.DoesNotContain((await List(client, "archived"))!, r => r.Id == id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/releases/{id}")).StatusCode);
    }

    [Fact]
    public async Task Remove_on_an_active_release_is_rejected()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Active Artist");
        var id = await CreateRelease(client, artist, "Still Active", Today.AddDays(10));

        var del = await client.DeleteAsync($"/api/releases/{id}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
    }

    [Fact]
    public async Task Archived_release_contributes_no_pending_actions()
    {
        using var factory = new ZmgApiFactory();
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
