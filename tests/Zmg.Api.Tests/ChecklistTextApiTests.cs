using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;
using Zmg.Infra.Data;

namespace Zmg.Api.Tests;

/// <summary>
/// v2.9 — checklist text is two plain columns the API ships as-is, and <c>Code</c>/<c>SourceCode</c> is
/// identity that nothing about text may disturb. Supersedes M47/M48's ChecklistTranslationApiTests and
/// TemplateTranslationEditApiTests, whose subject (per-locale rows, a request locale, edits that had to
/// infer intent) no longer exists.
/// </summary>
public class ChecklistTextApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    // The real seeded copy, not a fixture — asserting against what ships is what makes these tests
    // prove the shipped checklist rather than a hand-built one.
    private const string MixMasterEs = "Mezcla/master";
    private const string DistributeEs = "Distribuir a los DSPs";

    // ---- Seed ----

    [Fact]
    public void Every_seeded_template_task_carries_a_code_unique_within_its_template()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();

        var tasks = db.TemplateTasks.AsNoTracking().ToList();

        Assert.NotEmpty(tasks);
        Assert.All(tasks, t => Assert.False(string.IsNullOrWhiteSpace(t.Code)));
        foreach (var perTemplate in tasks.GroupBy(t => t.ChecklistTemplateId))
        {
            Assert.Equal(perTemplate.Count(), perTemplate.Select(t => t.Code).Distinct().Count());
        }
    }

    [Fact]
    public async Task A_release_is_stamped_with_source_codes_and_both_languages()
    {
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Stamped Release", TestDates.Today.AddDays(20));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();
        var tasks = db.ReleaseTasks.AsNoTracking().Where(t => t.ReleaseId == release.Id).ToList();

        Assert.NotEmpty(tasks);
        Assert.All(tasks, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.SourceCode));
            Assert.False(string.IsNullOrWhiteSpace(t.TitleEn));
            Assert.False(string.IsNullOrWhiteSpace(t.TitleEs));
        });
        Assert.Contains(tasks, t => t.SourceCode == TaskCodes.DistributeToDsps);
    }

    // ---- Both languages on the wire ----

    [Fact]
    public async Task Templates_carry_both_languages_in_one_response()
    {
        // No locale negotiation: the payload is the same whoever asks, and the SPA reads the column
        // matching what the user is reading. That is what makes a language switch a re-render and not
        // a refetch.
        var client = factory.CreateClient();

        var templates = (await client.GetFromJsonAsync<List<TemplateDto>>("/api/templates"))!;

        var single = templates.Single(t => t.Type == ReleaseType.Single).Phases.SelectMany(p => p.Tasks).ToList();
        var mix = single.Single(t => t.TitleEn == "Mix/master");
        Assert.Equal(MixMasterEs, mix.TitleEs);
    }

    [Fact]
    public async Task A_release_checklist_carries_both_languages_in_one_response()
    {
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Both Languages Release", TestDates.Today.AddDays(21));

        var distribute = release.Phases.SelectMany(p => p.Tasks)
            .Single(t => t.TitleEn == SeedData.DistributeToDspsEn);

        Assert.Equal(DistributeEs, distribute.TitleEs);
    }

    [Fact]
    public async Task A_task_added_without_spanish_stores_null_rather_than_a_blank()
    {
        // "Show the English" is one state, not two — a blank string and a null would render the same
        // and compare differently, which is how a fallback quietly becomes an empty checklist row.
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Blank Spanish Release", TestDates.Today.AddDays(22));

        var res = await client.PostAsJsonAsync($"/api/releases/{release.Id}/tasks",
            new AddTaskInput("Chase the mastering engineer", "   ", Phase.Pre));
        res.EnsureSuccessStatusCode();

        var created = (await res.Content.ReadFromJsonAsync<ReleaseTaskDto>())!;
        Assert.Equal("Chase the mastering engineer", created.TitleEn);
        Assert.Null(created.TitleEs);
    }

    // ---- Identity survives a text edit ----

    /// <summary>
    /// The bug v2.9 exists to make impossible. While <c>SourceCode</c> doubled as a translation join
    /// key, every title edit had to clear it — so rewording the DSP-distribution task on a release
    /// silently switched off <c>IsDistributed</c>, and with it the missing-UPC advisory, the pending
    /// engine and the past-date backfill. Nothing failed; the app just stopped noticing.
    /// </summary>
    [Fact]
    public async Task Rewording_the_distribute_task_keeps_the_release_distributed()
    {
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Reworded Release", TestDates.Today.AddDays(23));
        var task = release.Phases.SelectMany(p => p.Tasks)
            .Single(t => t.TitleEn == SeedData.DistributeToDspsEn);

        // Reword it in both languages, exactly as the task editor would.
        var updated = await client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new UpdateTaskInput("Send it to DistroKid", "Enviarlo a DistroKid", task.Phase, null));
        updated.EnsureSuccessStatusCode();

        // Check it off — this is what IsDistributed keys on.
        (await client.PatchAsync($"/api/tasks/{task.Id}/toggle", null)).EnsureSuccessStatusCode();

        // The code survived the rewording, so the rules still see the task for what it is.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();
            var stored = db.ReleaseTasks.AsNoTracking().Single(t => t.Id == task.Id);
            Assert.Equal(TaskCodes.DistributeToDsps, stored.SourceCode);
            Assert.Equal("Send it to DistroKid", stored.TitleEn);
            Assert.Equal("Enviarlo a DistroKid", stored.TitleEs);
        }

        // And the observable consequence: distributed with a blank UPC raises the advisory.
        var listed = await client.GetFromJsonAsync<List<ReleaseListItemDto>>("/api/releases?scope=all");
        Assert.Contains(ReleaseWarnings.MissingUpc, listed!.Single(r => r.Id == release.Id).Warnings);
    }

    [Fact]
    public async Task Moving_a_task_between_phases_leaves_both_texts_alone()
    {
        // The SPA round-trips the whole editable row on a phase move. That used to need a heuristic —
        // "is this an edit, or the title we just showed them?" — measured per locale. With both texts
        // sent explicitly there is nothing to infer.
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Phase Move Release", TestDates.Today.AddDays(24));
        var task = release.Phases.SelectMany(p => p.Tasks)
            .Single(t => t.TitleEn == SeedData.DistributeToDspsEn);

        var res = await client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new UpdateTaskInput(task.TitleEn, task.TitleEs, Phase.Release, null));
        res.EnsureSuccessStatusCode();

        var moved = (await res.Content.ReadFromJsonAsync<ReleaseTaskDto>())!;
        Assert.Equal(SeedData.DistributeToDspsEn, moved.TitleEn);
        Assert.Equal(DistributeEs, moved.TitleEs);
        Assert.Equal(Phase.Release, moved.Phase);
    }

    // ---- Helpers ----

    private static async Task<ReleaseDetailDto> CreateRelease(HttpClient client, string title, DateOnly date)
    {
        var artist = (await (await client.PostAsJsonAsync("/api/artists", new ArtistInput($"{title} Artist", null)))
            .Content.ReadFromJsonAsync<ArtistDto>())!;
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, date, artist.Id, null, null,
            new List<TrackInput> { new(null, $"{title} Song", null, null) }));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }
}
