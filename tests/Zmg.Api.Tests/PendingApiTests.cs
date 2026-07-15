using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>
/// M10 — the aggregate GET /api/pending ordering across releases, plus the
/// per-release GET /api/pending/{id} scoping.
/// </summary>
public class PendingApiTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<Guid> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!.Id;
    }

    private async Task CreateRelease(HttpClient client, Guid artistId, string title, DateOnly date) =>
        await CreateReleaseReturningId(client, artistId, title, date);

    private async Task<Guid> CreateReleaseReturningId(HttpClient client, Guid artistId, string title, DateOnly date)
    {
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, date, artistId, null, null,
            new List<TrackInput> { new(null, "Track 1", null, null) }));
        res.EnsureSuccessStatusCode();
        var created = (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!;
        return created.Data.Id;
    }

    [Fact]
    public async Task Pending_orders_task_due_nearest_first_then_data_items_last()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Pending Artist");

        // Two forward releases inside the seeded 7–14 day windows (Distribute + Pitch to Spotify),
        // and one past release (auto-distributed with blank ids → a missing-identifier data action).
        await CreateRelease(client, artist, "Near", Today.AddDays(3));
        await CreateRelease(client, artist, "Far", Today.AddDays(10));
        await CreateRelease(client, artist, "Past", Today.AddDays(-5));

        var pending = await client.GetFromJsonAsync<List<PendingAction>>("/api/pending");
        Assert.NotNull(pending);

        var taskDue = pending.Where(a => a.Kind == PendingKind.TaskDue).ToList();
        var missing = pending.Where(a => a.Kind == PendingKind.MissingIdentifier).ToList();

        // Every task-due item comes before every data item.
        var lastTaskDueIdx = pending.FindLastIndex(a => a.Kind == PendingKind.TaskDue);
        var firstMissingIdx = pending.FindIndex(a => a.Kind == PendingKind.MissingIdentifier);
        Assert.True(firstMissingIdx > lastTaskDueIdx);

        // Task-due items are ordered nearest-release-first (non-decreasing days-to-release).
        for (var i = 1; i < taskDue.Count; i++)
            Assert.True(taskDue[i - 1].DaysToRelease <= taskDue[i].DaysToRelease);
        Assert.Equal("Near", taskDue.First().ReleaseTitle);

        // The past release surfaces as a missing-identifier data action.
        Assert.Contains(missing, a => a.ReleaseTitle == "Past" && a.Label == "Missing UPC");
    }

    [Fact]
    public async Task Pending_by_release_returns_only_that_releases_actions()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "By Release Artist");

        // "Near" sits inside the seeded task-due windows; "Far" is another release whose
        // actions must not leak into the per-release result.
        var nearId = await CreateReleaseReturningId(client, artist, "Near", Today.AddDays(3));
        await CreateRelease(client, artist, "Far", Today.AddDays(10));

        var pending = await client.GetFromJsonAsync<List<PendingAction>>($"/api/pending/{nearId}");
        Assert.NotNull(pending);
        Assert.NotEmpty(pending);
        Assert.All(pending, a => Assert.Equal(nearId, a.ReleaseId));
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
