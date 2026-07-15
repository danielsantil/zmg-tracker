using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>M9 — the releases list scope (Home vs All), the All-Releases desc sort, and the title search.</summary>
public class ReleaseListScopeApiTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<Guid> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!.Id;
    }

    private async Task CreateRelease(HttpClient client, Guid artistId, string title, DateOnly date)
    {
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, date, artistId, null, null,
            new List<TrackInput> { new(null, "Track 1", null, null) }));
        res.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Scope_home_returns_only_releases_dated_today_or_later()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Home Artist");

        await CreateRelease(client, artist, "Past", Today.AddDays(-10));
        await CreateRelease(client, artist, "Today", Today);
        await CreateRelease(client, artist, "Future", Today.AddDays(30));

        var home = await client.GetFromJsonAsync<List<ReleaseListItemDto>>("/api/releases?scope=home");

        Assert.Contains(home!, r => r.Title == "Today");
        Assert.Contains(home!, r => r.Title == "Future");
        Assert.DoesNotContain(home!, r => r.Title == "Past");
        Assert.All(home!, r => Assert.True(r.ReleaseDate >= Today));
    }

    [Fact]
    public async Task Scope_all_returns_everything_ordered_by_release_date_desc()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "All Artist");

        await CreateRelease(client, artist, "Oldest", Today.AddDays(-20));
        await CreateRelease(client, artist, "Middle", Today.AddDays(5));
        await CreateRelease(client, artist, "Newest", Today.AddDays(40));

        var all = await client.GetFromJsonAsync<List<ReleaseListItemDto>>("/api/releases?scope=all");

        Assert.Equal(new[] { "Newest", "Middle", "Oldest" }, all!.Select(r => r.Title).ToArray());
    }

    [Fact]
    public async Task Title_search_is_case_insensitive_substring()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Search Artist");

        await CreateRelease(client, artist, "Summer Nights", Today.AddDays(1));
        await CreateRelease(client, artist, "Winter Days", Today.AddDays(2));

        var hits = await client.GetFromJsonAsync<List<ReleaseListItemDto>>("/api/releases?q=summer");

        Assert.Single(hits!);
        Assert.Equal("Summer Nights", hits!.Single().Title);
    }
}
