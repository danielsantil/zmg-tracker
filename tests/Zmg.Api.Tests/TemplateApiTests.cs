using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

// A fresh factory (isolated in-memory DB) per test: these tests mutate the shared
// seeded templates, so they can't share one database the way the release-task tests do.
public class TemplateApiTests : IDisposable
{
    private readonly ZmgApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static async Task<TemplateDto> GetTemplate(HttpClient client, ReleaseType type)
    {
        var templates = await client.GetFromJsonAsync<List<TemplateDto>>("/api/templates");
        return templates!.Single(t => t.Type == type);
    }

    private static int TotalTasks(TemplateDto t) => t.Phases.Sum(p => p.Tasks.Count);

    [Fact]
    public async Task List_returns_both_templates_with_seeded_counts()
    {
        var client = _factory.CreateClient();

        var templates = await client.GetFromJsonAsync<List<TemplateDto>>("/api/templates");

        Assert.Equal(2, templates!.Count);
        var single = templates.Single(t => t.Type == ReleaseType.Single);
        var album = templates.Single(t => t.Type == ReleaseType.Album);
        Assert.Equal(31, TotalTasks(single));
        Assert.Equal(41, TotalTasks(album));
        Assert.Equal(6, single.Phases.Single(p => p.Phase == Phase.Pre).Tasks.Count);
    }

    [Fact]
    public async Task Add_template_task_appends_to_phase()
    {
        var client = _factory.CreateClient();
        var single = await GetTemplate(client, ReleaseType.Single);
        var preBefore = single.Phases.Single(p => p.Phase == Phase.Pre).Tasks.Count;

        var res = await client.PostAsJsonAsync($"/api/templates/{single.Id}/tasks",
            new AddTemplateTaskInput("Custom pre step", Phase.Pre));
        res.EnsureSuccessStatusCode();
        var created = (await res.Content.ReadFromJsonAsync<TemplateTaskDto>())!;
        Assert.Equal(Phase.Pre, created.Phase);

        var after = await GetTemplate(client, ReleaseType.Single);
        var pre = after.Phases.Single(p => p.Phase == Phase.Pre);
        Assert.Equal(preBefore + 1, pre.Tasks.Count);
        Assert.Equal(created.SortOrder, pre.Tasks.Max(t => t.SortOrder));
    }

    [Fact]
    public async Task Add_template_task_with_blank_title_is_rejected()
    {
        var client = _factory.CreateClient();
        var single = await GetTemplate(client, ReleaseType.Single);

        var res = await client.PostAsJsonAsync($"/api/templates/{single.Id}/tasks",
            new AddTemplateTaskInput("   ", Phase.Pre));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Update_template_task_can_rename_and_move_phase()
    {
        var client = _factory.CreateClient();
        var single = await GetTemplate(client, ReleaseType.Single);
        var task = single.Phases.Single(p => p.Phase == Phase.Pre).Tasks.First();

        var res = await client.PutAsJsonAsync($"/api/template-tasks/{task.Id}",
            new UpdateTemplateTaskInput("Renamed step", Phase.Post));
        res.EnsureSuccessStatusCode();
        var updated = (await res.Content.ReadFromJsonAsync<TemplateTaskDto>())!;

        Assert.Equal("Renamed step", updated.Title);
        Assert.Equal(Phase.Post, updated.Phase);
    }

    [Fact]
    public async Task Reorder_template_phase_persists()
    {
        var client = _factory.CreateClient();
        var single = await GetTemplate(client, ReleaseType.Single);
        var pre = single.Phases.Single(p => p.Phase == Phase.Pre).Tasks
            .OrderBy(t => t.SortOrder).ToList();
        var reversedIds = pre.Select(t => t.Id).Reverse().ToList();

        var res = await client.PutAsJsonAsync($"/api/templates/{single.Id}/tasks/order",
            new ReorderTemplateTasksInput(Phase.Pre, reversedIds));
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var after = await GetTemplate(client, ReleaseType.Single);
        var afterIds = after.Phases.Single(p => p.Phase == Phase.Pre).Tasks
            .OrderBy(t => t.SortOrder).Select(t => t.Id).ToList();
        Assert.Equal(reversedIds, afterIds);
    }

    [Fact]
    public async Task Reorder_with_missing_ids_is_rejected()
    {
        var client = _factory.CreateClient();
        var single = await GetTemplate(client, ReleaseType.Single);
        var pre = single.Phases.Single(p => p.Phase == Phase.Pre).Tasks;

        var res = await client.PutAsJsonAsync($"/api/templates/{single.Id}/tasks/order",
            new ReorderTemplateTasksInput(Phase.Pre, new List<Guid> { pre.First().Id }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Delete_template_task_removes_it_and_lowers_count()
    {
        var client = _factory.CreateClient();
        var single = await GetTemplate(client, ReleaseType.Single);
        var before = TotalTasks(single);
        var task = single.Phases.Single(p => p.Phase == Phase.Pre).Tasks.First();

        var del = await client.DeleteAsync($"/api/template-tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var after = await GetTemplate(client, ReleaseType.Single);
        Assert.Equal(before - 1, TotalTasks(after));
        Assert.DoesNotContain(after.Phases.SelectMany(p => p.Tasks), t => t.Id == task.Id);
    }

    [Fact]
    public async Task Delete_missing_template_task_is_not_found()
    {
        var client = _factory.CreateClient();
        var res = await client.DeleteAsync($"/api/template-tasks/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Editing_a_template_does_not_touch_existing_releases()
    {
        var client = _factory.CreateClient();

        // Create an artist + release; the release snapshots the Single template on create.
        var artist = (await (await client.PostAsJsonAsync("/api/artists",
            new ArtistInput("Invariant Artist", null))).Content.ReadFromJsonAsync<ArtistDto>())!;
        var release = (await (await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            "Invariant Song", ReleaseType.Single, new DateOnly(2026, 8, 14), artist.Id, null, null,
            new List<TrackInput> { new(null, "Track 1", null, null) })))
            .Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
        var releaseTotalBefore = release.TotalTasks;

        // Mutate the Single template every way: add, rename, delete.
        var single = await GetTemplate(client, ReleaseType.Single);
        await client.PostAsJsonAsync($"/api/templates/{single.Id}/tasks",
            new AddTemplateTaskInput("Brand new template task", Phase.Pre));
        var renameTarget = single.Phases.Single(p => p.Phase == Phase.Release).Tasks.First();
        await client.PutAsJsonAsync($"/api/template-tasks/{renameTarget.Id}",
            new UpdateTemplateTaskInput("Template task renamed", Phase.Release));
        var deleteTarget = single.Phases.Single(p => p.Phase == Phase.Post).Tasks.First();
        await client.DeleteAsync($"/api/template-tasks/{deleteTarget.Id}");

        // The existing release's checklist is unchanged: same count, still has the
        // task whose template source was renamed, no sign of the new template task.
        var after = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{release.Id}");
        Assert.Equal(releaseTotalBefore, after!.TotalTasks);
        var titles = after.Phases.SelectMany(p => p.Tasks).Select(t => t.Title).ToList();
        Assert.Contains(renameTarget.Title, titles);
        Assert.DoesNotContain("Template task renamed", titles);
        Assert.DoesNotContain("Brand new template task", titles);
    }
}
