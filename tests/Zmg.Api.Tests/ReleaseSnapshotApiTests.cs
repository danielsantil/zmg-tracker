using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Tests;

/// <summary>
/// "Changes apply to future releases only — existing releases keep their own copy", in <b>both</b>
/// languages, plus its mirror: editing a release never reaches the template. Its own class, and so its
/// own factory/database — these tests mutate the seeded templates.
/// </summary>
/// <remarks>
/// The reported v2.8 bug this pins: renaming a task's Spanish in the templates screen rewrote the
/// Spanish of every release already created from it, while its English stayed put, because release
/// tasks resolved their non-English text live from the *template's* rows through a shared code. Since
/// v2.9 both languages are columns the release owns, so the asymmetry that allowed it is gone — but the
/// promise is worth a test that doesn't care how it's kept.
/// </remarks>
public class ReleaseSnapshotApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private const string MixMasterEs = "Mezcla/master";

    [Fact]
    public async Task Editing_a_template_leaves_existing_releases_alone_in_both_languages()
    {
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Snapshot Release", TestDates.Today.AddDays(40));
        var templateTask = await SingleTemplateTask(client, "Mix/master");

        await PutTemplateTask(client, templateTask.Id,
            "Mix and master the record", "Mezclar y masterizar el disco", templateTask.Phase);

        // The existing release keeps the text it was created with, in both languages.
        var tasks = (await GetRelease(client, release.Id)).Phases.SelectMany(p => p.Tasks).ToList();
        var mix = tasks.Single(t => t.TitleEn == "Mix/master");
        Assert.Equal("Mix/master", mix.TitleEn);
        Assert.Equal(MixMasterEs, mix.TitleEs);

        // ...and the edit still does what it's for: the next release picks it up, in both languages.
        var later = await CreateRelease(client, "Post-edit Release", TestDates.Today.AddDays(41));
        var laterMix = later.Phases.SelectMany(p => p.Tasks).Single(t => t.TitleEn == "Mix and master the record");
        Assert.Equal("Mezclar y masterizar el disco", laterMix.TitleEs);
    }

    [Fact]
    public async Task Editing_a_release_task_leaves_the_template_alone_in_both_languages()
    {
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Custom Task Release", TestDates.Today.AddDays(42));
        var task = release.Phases.SelectMany(p => p.Tasks)
            .Single(t => t.TitleEn == SeedData.DistributeToDspsEn);

        var res = await client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new UpdateTaskInput("Upload to the stores", "Subir a las tiendas", task.Phase, null));
        res.EnsureSuccessStatusCode();

        // The template still ships the standard, untouched, in both languages.
        var templateTask = await SingleTemplateTask(client, SeedData.DistributeToDspsEn);
        Assert.Equal("Distribuir a los DSPs", templateTask.TitleEs);
    }

    /// <summary>
    /// v2.9 makes template edits per-template. The base checklist is seeded into both templates as
    /// separate rows, so correcting the Single tab is a Single-tab change — which is what a two-tab
    /// editor implies, and it makes English and Spanish edits behave identically. (v2.8's Spanish edits
    /// fanned out across templates while its English edits did not, which was a bug source in itself.)
    /// </summary>
    [Fact]
    public async Task Editing_a_task_on_one_template_leaves_the_other_template_alone()
    {
        var client = factory.CreateClient();
        var task = await SingleTemplateTask(client, "Pitch to Spotify");

        await PutTemplateTask(client, task.Id, "Pitch to Spotify editorial", "Pitch editorial a Spotify", task.Phase);

        var albumTask = (await GetTemplates(client)).Single(t => t.Type == ReleaseType.Album)
            .Phases.SelectMany(p => p.Tasks).Single(t => t.TitleEn == "Pitch to Spotify");
        Assert.Equal("Pitch a Spotify", albumTask.TitleEs);
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

    private static async Task<ReleaseDetailDto> GetRelease(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<ReleaseDetailDto>($"/api/releases/{id}"))!;

    private static async Task<List<TemplateDto>> GetTemplates(HttpClient client) =>
        (await client.GetFromJsonAsync<List<TemplateDto>>("/api/templates"))!;

    private static async Task<TemplateTaskDto> SingleTemplateTask(HttpClient client, string titleEn) =>
        (await GetTemplates(client)).Single(t => t.Type == ReleaseType.Single)
            .Phases.SelectMany(p => p.Tasks)
            .Single(t => t.TitleEn == titleEn);

    private static async Task PutTemplateTask(
        HttpClient client, Guid id, string titleEn, string? titleEs, Phase phase)
    {
        var res = await client.PutAsJsonAsync($"/api/template-tasks/{id}",
            new UpdateTemplateTaskInput(titleEn, titleEs, phase));
        res.EnsureSuccessStatusCode();
    }
}
