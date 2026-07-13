using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>M7 — UPC/ISRC round-trip, the soft identifier warning, and past-date backfill auto-check.</summary>
public class ReleaseIdentifierApiTests : IClassFixture<ZmgApiFactory>
{
    private readonly ZmgApiFactory _factory;

    public ReleaseIdentifierApiTests(ZmgApiFactory factory) => _factory = factory;

    private async Task<ArtistDto> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!;
    }

    private async Task<ReleaseDetailDto> CreateRelease(
        HttpClient client, Guid artistId, string title, DateOnly date, string? upc = null, string? isrc = null)
    {
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, date, artistId, null, null, null, Upc: upc, Isrc: isrc));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }

    private static ReleaseTaskDto DistributeTask(ReleaseDetailDto detail) =>
        detail.Phases.SelectMany(p => p.Tasks).Single(t => t.Title == SeedData.DistributeToDspsTitle);

    [Fact]
    public async Task Upc_and_isrc_round_trip_on_create_and_update()
    {
        var client = _factory.CreateClient();
        var artist = await CreateArtist(client, "Identifier Artist");

        var created = await CreateRelease(client, artist.Id, "IdSong", new DateOnly(2026, 8, 14),
            upc: "0123456789012", isrc: "US-ABC-26-00001");
        Assert.Equal("0123456789012", created.Upc);
        Assert.Equal("US-ABC-26-00001", created.Isrc);

        var upd = await client.PutAsJsonAsync($"/api/releases/{created.Id}", new ReleaseInput(
            "IdSong", ReleaseType.Single, new DateOnly(2026, 8, 14), artist.Id, null, null, null,
            Upc: "9999999999999", Isrc: "US-ABC-26-99999"));
        upd.EnsureSuccessStatusCode();
        var updated = (await upd.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
        Assert.Equal("9999999999999", updated.Upc);
        Assert.Equal("US-ABC-26-99999", updated.Isrc);
    }

    [Fact]
    public async Task NeedsIdentifierWarning_only_after_distribution_and_clears_when_ids_filled()
    {
        var client = _factory.CreateClient();
        var artist = await CreateArtist(client, "Warning Artist");

        // Future release, blank ids: silent until distributed.
        var created = await CreateRelease(client, artist.Id, "WarnSong", new DateOnly(2026, 8, 14));
        Assert.False(created.NeedsIdentifierWarning);

        // Check "Distribute to DSPs" — now a blank id surfaces the warning.
        var distribute = DistributeTask(created);
        await client.PatchAsync($"/api/tasks/{distribute.Id}/toggle", null);

        var afterDistribute = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{created.Id}");
        Assert.True(afterDistribute!.NeedsIdentifierWarning);

        // The list flag agrees without an extra fetch.
        var list = await client.GetFromJsonAsync<List<ReleaseListItemDto>>($"/api/releases?artistId={artist.Id}");
        Assert.True(list!.Single(r => r.Id == created.Id).NeedsIdentifierWarning);

        // Fill both ids: warning clears.
        var upd = await client.PutAsJsonAsync($"/api/releases/{created.Id}", new ReleaseInput(
            "WarnSong", ReleaseType.Single, new DateOnly(2026, 8, 14), artist.Id, null, null, null,
            Upc: "0123456789012", Isrc: "US-ABC-26-00001"));
        upd.EnsureSuccessStatusCode();
        var filled = (await upd.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
        Assert.False(filled.NeedsIdentifierWarning);
    }

    [Fact]
    public async Task Create_with_past_date_auto_checks_distribute_to_dsps()
    {
        var client = _factory.CreateClient();
        var artist = await CreateArtist(client, "Backfill Artist");

        var created = await CreateRelease(client, artist.Id, "OldSong", new DateOnly(2026, 6, 1));

        // Only "Distribute to DSPs" is auto-checked; everything else stays open.
        var distribute = DistributeTask(created);
        Assert.True(distribute.IsDone);
        Assert.Equal(1, created.DoneTasks);

        // Blank ids on an already-distributed release surface the warning.
        Assert.True(created.NeedsIdentifierWarning);
    }
}
