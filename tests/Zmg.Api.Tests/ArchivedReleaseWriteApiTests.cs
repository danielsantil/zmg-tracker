using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>
/// M25 defect 1: an archived release is terminal and read-only. Every write path — the release PUT,
/// task mutations, and track mutations — must return 409 once the release is archived. The whole read
/// side already assumed this; nothing enforced it before. One test per service proves the wiring.
/// </summary>
public class ArchivedReleaseWriteApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<Guid> CreateArtist(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput(name, null));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ArtistDto>())!.Id;
    }

    // Create a two-track album dated in the future, capture its detail (for task/track ids), then archive it.
    private async Task<ReleaseDetailDto> CreateArchivedAlbum(HttpClient client, string artistName)
    {
        var artist = await CreateArtist(client, artistName);
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Archived Album", ReleaseType.Album, Today.AddDays(30), artist, null, null,
            new List<TrackInput> { new(null, "One", null, null), new(null, "Two", null, null) }));
        res.EnsureSuccessStatusCode();
        var detail = (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;

        (await client.PostAsync($"/api/releases/{detail.Id}/archive", null)).EnsureSuccessStatusCode();
        return detail;
    }

    [Fact]
    public async Task Put_on_an_archived_release_is_rejected()
    {
        var client = factory.CreateClient();
        var detail = await CreateArchivedAlbum(client, "Archived PUT Artist");

        var res = await client.PutAsJsonAsync($"/api/releases/{detail.Id}", new ReleaseInput(
            "Renamed", ReleaseType.Album, detail.ReleaseDate, detail.MainArtistId, null, null, null));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Adding_a_task_to_an_archived_release_is_rejected()
    {
        var client = factory.CreateClient();
        var detail = await CreateArchivedAlbum(client, "Archived Add-Task Artist");

        var res = await client.PostAsJsonAsync($"/api/releases/{detail.Id}/tasks",
            new AddTaskInput("Late task", Phase.Pre));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Toggling_a_task_on_an_archived_release_is_rejected()
    {
        var client = factory.CreateClient();
        var detail = await CreateArchivedAlbum(client, "Archived Toggle-Task Artist");
        var taskId = detail.Phases.SelectMany(p => p.Tasks).First().Id;

        var res = await client.PatchAsync($"/api/tasks/{taskId}/toggle", null);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Adding_a_track_to_an_archived_release_is_rejected()
    {
        var client = factory.CreateClient();
        var detail = await CreateArchivedAlbum(client, "Archived Add-Track Artist");

        var res = await client.PostAsJsonAsync($"/api/releases/{detail.Id}/tracks",
            new TrackInput(null, "New Track", null, null));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Toggling_track_focus_on_an_archived_release_is_rejected()
    {
        var client = factory.CreateClient();
        var detail = await CreateArchivedAlbum(client, "Archived Focus Artist");
        var songId = detail.Tracks.First().SongId;

        var res = await client.PatchAsync($"/api/releases/{detail.Id}/tracks/{songId}/focus", null);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Removing_a_track_from_an_archived_release_is_rejected()
    {
        var client = factory.CreateClient();
        var detail = await CreateArchivedAlbum(client, "Archived Remove-Track Artist");
        var songId = detail.Tracks.First().SongId;

        var res = await client.DeleteAsync($"/api/releases/{detail.Id}/tracks/{songId}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }
}
