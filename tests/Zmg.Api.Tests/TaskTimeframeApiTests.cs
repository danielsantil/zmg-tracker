using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>M8 — task + template-task endpoints accept and round-trip the days-before timeframe.</summary>
// A fresh factory per test: the template-task cases mutate the shared seeded templates.
public class TaskTimeframeApiTests : IDisposable
{
    private readonly ZmgApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<ReleaseDetailDto> CreateRelease(HttpClient client, string artistName, string title)
    {
        var artistRes = await client.PostAsJsonAsync("/api/artists", new ArtistInput(artistName, null));
        artistRes.EnsureSuccessStatusCode();
        var artist = (await artistRes.Content.ReadFromJsonAsync<ArtistDto>())!;

        var relRes = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, new DateOnly(2026, 8, 14), artist.Id, null, null,
            new List<TrackInput> { new(null, "Track 1", null, null) }));
        relRes.EnsureSuccessStatusCode();
        return (await relRes.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }

    [Fact]
    public async Task Add_task_with_timeframe_round_trips()
    {
        var client = _factory.CreateClient();
        var release = await CreateRelease(client, "Timeframe Artist", "Timeframe Song");

        var res = await client.PostAsJsonAsync($"/api/releases/{release.Id}/tasks",
            new AddTaskInput("Distribute somewhere", Phase.Pre, MinDaysBefore: 7, MaxDaysBefore: 14));
        res.EnsureSuccessStatusCode();
        var created = (await res.Content.ReadFromJsonAsync<ReleaseTaskDto>())!;

        Assert.Equal(7, created.MinDaysBefore);
        Assert.Equal(14, created.MaxDaysBefore);

        var detail = await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{release.Id}");
        var reloaded = detail!.Phases.SelectMany(p => p.Tasks).Single(t => t.Id == created.Id);
        Assert.Equal(7, reloaded.MinDaysBefore);
        Assert.Equal(14, reloaded.MaxDaysBefore);
    }

    [Fact]
    public async Task Update_task_sets_and_clears_timeframe()
    {
        var client = _factory.CreateClient();
        var release = await CreateRelease(client, "Update TF Artist", "Update TF Song");
        var task = release.Phases.Single(p => p.Phase == Phase.Pre).Tasks.First();

        var set = await client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new UpdateTaskInput(task.Title, task.Phase, null, MinDaysBefore: 3, MaxDaysBefore: 10));
        set.EnsureSuccessStatusCode();
        var afterSet = (await set.Content.ReadFromJsonAsync<ReleaseTaskDto>())!;
        Assert.Equal(3, afterSet.MinDaysBefore);
        Assert.Equal(10, afterSet.MaxDaysBefore);

        // Omitting the timeframe (defaults null) clears it — the UI always sends the current value.
        var clear = await client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new UpdateTaskInput(task.Title, task.Phase, null));
        clear.EnsureSuccessStatusCode();
        var afterClear = (await clear.Content.ReadFromJsonAsync<ReleaseTaskDto>())!;
        Assert.Null(afterClear.MinDaysBefore);
        Assert.Null(afterClear.MaxDaysBefore);
    }

    [Fact]
    public async Task Add_template_task_with_timeframe_round_trips()
    {
        var client = _factory.CreateClient();
        var templates = await client.GetFromJsonAsync<List<TemplateDto>>("/api/templates");
        var single = templates!.Single(t => t.Type == ReleaseType.Single);

        var res = await client.PostAsJsonAsync($"/api/templates/{single.Id}/tasks",
            new AddTemplateTaskInput("Custom pre with window", Phase.Pre, MinDaysBefore: 5, MaxDaysBefore: 21));
        res.EnsureSuccessStatusCode();
        var created = (await res.Content.ReadFromJsonAsync<TemplateTaskDto>())!;

        Assert.Equal(5, created.MinDaysBefore);
        Assert.Equal(21, created.MaxDaysBefore);

        var reloaded = await client.GetFromJsonAsync<List<TemplateDto>>("/api/templates");
        var task = reloaded!.Single(t => t.Type == ReleaseType.Single)
            .Phases.SelectMany(p => p.Tasks).Single(t => t.Id == created.Id);
        Assert.Equal(5, task.MinDaysBefore);
        Assert.Equal(21, task.MaxDaysBefore);
    }

    [Fact]
    public async Task Update_template_task_sets_timeframe()
    {
        var client = _factory.CreateClient();
        var templates = await client.GetFromJsonAsync<List<TemplateDto>>("/api/templates");
        var single = templates!.Single(t => t.Type == ReleaseType.Single);
        var task = single.Phases.Single(p => p.Phase == Phase.Pre).Tasks.First();

        var res = await client.PutAsJsonAsync($"/api/template-tasks/{task.Id}",
            new UpdateTemplateTaskInput(task.Title, task.Phase, MinDaysBefore: 2, MaxDaysBefore: 9));
        res.EnsureSuccessStatusCode();
        var updated = (await res.Content.ReadFromJsonAsync<TemplateTaskDto>())!;

        Assert.Equal(2, updated.MinDaysBefore);
        Assert.Equal(9, updated.MaxDaysBefore);
    }
}
