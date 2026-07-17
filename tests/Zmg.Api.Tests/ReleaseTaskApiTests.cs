using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

public class ReleaseTaskApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private async Task<ReleaseDetailDto> CreateReleaseWithChecklist(HttpClient client, string artistName, string title)
    {
        var artistRes = await client.PostAsJsonAsync("/api/artists", new ArtistInput(artistName, null));
        artistRes.EnsureSuccessStatusCode();
        var artist = (await artistRes.Content.ReadFromJsonAsync<ArtistDto>())!;

        var relRes = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, TestDates.Upcoming, artist.Id, null, null,
            new List<TrackInput> { new(null, "Track 1", null, null) }));
        relRes.EnsureSuccessStatusCode();
        return (await relRes.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }

    private static ReleaseTaskDto FirstTask(ReleaseDetailDto detail) =>
        detail.Phases.SelectMany(p => p.Tasks).First();

    [Fact]
    public async Task Toggle_marks_task_done_and_stamps_completed_then_clears()
    {
        var client = factory.CreateClient();
        var release = await CreateReleaseWithChecklist(client, "Toggle Artist", "Toggle Song");
        var task = FirstTask(release);

        var on = await client.PatchAsync($"/api/tasks/{task.Id}/toggle", null);
        on.EnsureSuccessStatusCode();
        var afterOn = (await on.Content.ReadFromJsonAsync<ReleaseTaskDto>())!;
        Assert.True(afterOn.IsDone);
        Assert.NotNull(afterOn.CompletedAt);

        var off = await client.PatchAsync($"/api/tasks/{task.Id}/toggle", null);
        var afterOff = (await off.Content.ReadFromJsonAsync<ReleaseTaskDto>())!;
        Assert.False(afterOff.IsDone);
        Assert.Null(afterOff.CompletedAt);
    }

    [Fact]
    public async Task Add_task_appends_to_phase_and_shows_in_detail()
    {
        var client = factory.CreateClient();
        var release = await CreateReleaseWithChecklist(client, "Add Artist", "Add Song");
        var preBefore = release.Phases.Single(p => p.Phase == Phase.Pre).Total;

        var res = await client.PostAsJsonAsync($"/api/releases/{release.Id}/tasks",
            new AddTaskInput("Custom pre task", Phase.Pre));
        res.EnsureSuccessStatusCode();
        var created = (await res.Content.ReadFromJsonAsync<ReleaseTaskDto>())!;
        Assert.Equal(Phase.Pre, created.Phase);

        var detail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{release.Id}");
        var preAfter = detail!.Phases.Single(p => p.Phase == Phase.Pre);
        Assert.Equal(preBefore + 1, preAfter.Total);
        Assert.Equal(created.SortOrder, preAfter.Tasks.Max(t => t.SortOrder));
    }

    [Fact]
    public async Task Add_task_with_blank_title_is_rejected()
    {
        var client = factory.CreateClient();
        var release = await CreateReleaseWithChecklist(client, "Blank Artist", "Blank Song");

        var res = await client.PostAsJsonAsync($"/api/releases/{release.Id}/tasks",
            new AddTaskInput("   ", Phase.Pre));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Update_task_can_rename_and_move_phase()
    {
        var client = factory.CreateClient();
        var release = await CreateReleaseWithChecklist(client, "Update Artist", "Update Song");
        var task = release.Phases.Single(p => p.Phase == Phase.Pre).Tasks.First();

        var res = await client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new UpdateTaskInput("Renamed & moved", Phase.Post, "with notes"));
        res.EnsureSuccessStatusCode();
        var updated = (await res.Content.ReadFromJsonAsync<ReleaseTaskDto>())!;

        Assert.Equal("Renamed & moved", updated.Title);
        Assert.Equal(Phase.Post, updated.Phase);
        Assert.Equal("with notes", updated.Notes);
    }

    [Fact]
    public async Task Reorder_reverses_phase_and_persists()
    {
        var client = factory.CreateClient();
        var release = await CreateReleaseWithChecklist(client, "Reorder Artist", "Reorder Song");
        var pre = release.Phases.Single(p => p.Phase == Phase.Pre).Tasks
            .OrderBy(t => t.SortOrder).ToList();
        var reversedIds = pre.Select(t => t.Id).Reverse().ToList();

        var res = await client.PutAsJsonAsync($"/api/releases/{release.Id}/tasks/order",
            new ReorderTasksInput(Phase.Pre, reversedIds));
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var detail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{release.Id}");
        var afterIds = detail!.Phases.Single(p => p.Phase == Phase.Pre).Tasks
            .OrderBy(t => t.SortOrder).Select(t => t.Id).ToList();
        Assert.Equal(reversedIds, afterIds);
    }

    [Fact]
    public async Task Reorder_with_missing_ids_is_rejected()
    {
        var client = factory.CreateClient();
        var release = await CreateReleaseWithChecklist(client, "Bad Reorder Artist", "Bad Reorder Song");
        var pre = release.Phases.Single(p => p.Phase == Phase.Pre).Tasks;

        var res = await client.PutAsJsonAsync($"/api/releases/{release.Id}/tasks/order",
            new ReorderTasksInput(Phase.Pre, new List<Guid> { pre.First().Id }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Delete_task_removes_it_and_lowers_total()
    {
        var client = factory.CreateClient();
        var release = await CreateReleaseWithChecklist(client, "Delete Artist", "Delete Song");
        var task = FirstTask(release);

        var del = await client.DeleteAsync($"/api/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var detail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{release.Id}");
        Assert.Equal(30, detail!.TotalTasks);
        Assert.DoesNotContain(detail.Phases.SelectMany(p => p.Tasks), t => t.Id == task.Id);
    }

    [Fact]
    public async Task Toggle_missing_task_is_not_found()
    {
        var client = factory.CreateClient();
        var res = await client.PatchAsync($"/api/tasks/{Guid.NewGuid()}/toggle", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
