using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>M7 (v2.0: UPC-only) — UPC round-trip, the soft identifier warning, and past-date backfill
/// auto-check. ISRC moved to the song (see catalog tests, M13).</summary>
public class ReleaseIdentifierApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private async Task<ArtistDto> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!;
    }

    private async Task<ReleaseDetailDto> CreateRelease(
        HttpClient client, Guid artistId, string title, DateOnly date, string? upc = null)
    {
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, date, artistId, null, null,
            new List<TrackInput> { new(null, "Track 1", null, null) }, Upc: upc));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }

    private static ReleaseTaskDto DistributeTask(ReleaseDetailDto detail) =>
        detail.Phases.SelectMany(p => p.Tasks).Single(t => t.Title == SeedData.DistributeToDspsTitle);

    [Fact]
    public async Task Upc_round_trips_on_create_and_update()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Identifier Artist");

        var created = await CreateRelease(client, artist.Id, "IdSong", TestDates.Upcoming,
            upc: "0123456789012");
        Assert.Equal("0123456789012", created.Upc);

        var upd = await client.PutAsJsonAsync($"/api/releases/{created.Id}", new ReleaseInput(
            "IdSong", ReleaseType.Single, TestDates.Upcoming, artist.Id, null, null, null,
            Upc: "9999999999999"));
        upd.EnsureSuccessStatusCode();
        var updated = (await upd.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
        Assert.Equal("9999999999999", updated.Upc);
    }

    [Fact]
    public async Task NeedsIdentifierWarning_only_after_distribution_and_clears_when_upc_filled()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Warning Artist");

        // Future release, blank UPC: silent until distributed.
        var created = await CreateRelease(client, artist.Id, "WarnSong", TestDates.Upcoming);
        Assert.DoesNotContain(ReleaseWarnings.MissingUpc, created.Warnings);

        // Check "Distribute to DSPs" — now a blank UPC surfaces the warning.
        var distribute = DistributeTask(created);
        await client.PatchAsync($"/api/tasks/{distribute.Id}/toggle", null);

        var afterDistribute = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{created.Id}");
        Assert.Contains(ReleaseWarnings.MissingUpc, afterDistribute!.Warnings);

        // The list flag agrees without an extra fetch.
        var list = await client.GetFromJsonAsync<List<ReleaseListItemDto>>($"/api/releases?artistId={artist.Id}");
        Assert.Contains(ReleaseWarnings.MissingUpc, list!.Single(r => r.Id == created.Id).Warnings);

        // Fill the UPC: warning clears.
        var upd = await client.PutAsJsonAsync($"/api/releases/{created.Id}", new ReleaseInput(
            "WarnSong", ReleaseType.Single, TestDates.Upcoming, artist.Id, null, null, null,
            Upc: "0123456789012"));
        upd.EnsureSuccessStatusCode();
        var filled = (await upd.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
        Assert.DoesNotContain(ReleaseWarnings.MissingUpc, filled.Warnings);
    }

    [Fact]
    public async Task Create_with_past_date_auto_checks_distribute_to_dsps()
    {
        var client = factory.CreateClient();
        var artist = await CreateArtist(client, "Backfill Artist");

        var created = await CreateRelease(client, artist.Id, "OldSong", new DateOnly(2026, 6, 1));

        // Only "Distribute to DSPs" is auto-checked; everything else stays open.
        var distribute = DistributeTask(created);
        Assert.True(distribute.IsDone);
        Assert.Equal(1, created.DoneTasks);

        // Blank UPC on an already-distributed release surfaces the warning.
        Assert.Contains(ReleaseWarnings.MissingUpc, created.Warnings);
    }
}
