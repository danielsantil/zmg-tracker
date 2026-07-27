using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;
using Zmg.Infra.Data;

namespace Zmg.Api.Tests;

/// <summary>
/// "Changes apply to future releases only — existing releases keep their own copy", in every language.
/// Its own class, and so its own factory/database: these tests need the seeded template text pristine,
/// and <see cref="TemplateTranslationEditApiTests"/> edits it.
/// </summary>
public class ReleaseSnapshotApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private const string MixMasterEs = "Mezcla/master";
    private const string DistributeEs = "Distribuir a los DSPs";

    /// <summary>
    /// Reported bug: renaming a task's Spanish in the templates screen rewrote the Spanish of every
    /// release already created from it, while its English stayed put — because release tasks resolved
    /// their non-English text live from the *template's* rows. The templates screen promises "changes
    /// apply to future releases only"; this is that promise, in Spanish.
    /// </summary>
    [Fact]
    public async Task Editing_a_template_in_spanish_leaves_existing_releases_alone()
    {
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Snapshot Release", TestDates.Today.AddDays(40));
        var templateTask = await SingleTemplateTask(client, "es", MixMasterEs);

        await PutTemplateTask(client, templateTask.Id, "Mezcla/master esp 22", templateTask.Phase, "es");

        // The existing release keeps the text it was created with, in both languages.
        var spanish = await GetRelease(client, release.Id, "es");
        Assert.Contains(spanish.Phases.SelectMany(p => p.Tasks), t => t.Title == MixMasterEs);
        Assert.DoesNotContain(spanish.Phases.SelectMany(p => p.Tasks), t => t.Title == "Mezcla/master esp 22");
        var english = await GetRelease(client, release.Id, "en");
        Assert.Contains(english.Phases.SelectMany(p => p.Tasks), t => t.Title == "Mix/master");

        // ...and the edit still does what it's for: the next release picks it up.
        var later = await CreateRelease(client, "Post-edit Release", TestDates.Today.AddDays(41));
        var laterSpanish = await GetRelease(client, later.Id, "es");
        Assert.Contains(laterSpanish.Phases.SelectMany(p => p.Tasks), t => t.Title == "Mezcla/master esp 22");
    }

    /// <summary>Renaming a release task makes it custom — one text everywhere, snapshot rows dropped.</summary>
    [Fact]
    public async Task Renaming_a_release_task_in_spanish_drops_its_snapshot_rows()
    {
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Custom Task Release", TestDates.Today.AddDays(42));
        var task = (await GetRelease(client, release.Id, "es"))
            .Phases.SelectMany(p => p.Tasks).Single(t => t.Title == DistributeEs);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tasks/{task.Id}")
        {
            Content = JsonContent.Create(new UpdateTaskInput("Subir a las tiendas", task.Phase, null)),
        };
        request.Headers.Add("X-Lang", "es");
        (await client.SendAsync(request)).EnsureSuccessStatusCode();

        // Both languages show the user's own text — a leftover snapshot row would override it on a switch.
        foreach (var locale in new[] { "en", "es" })
        {
            var tasks = (await GetRelease(client, release.Id, locale)).Phases.SelectMany(p => p.Tasks).ToList();
            Assert.Contains(tasks, t => t.Title == "Subir a las tiendas");
            Assert.DoesNotContain(tasks, t => t.Title is DistributeEs or "Distribute to DSPs");
        }

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();
        Assert.False(db.ReleaseTaskTranslations.Any(t => t.ReleaseTaskId == task.Id));
        // And it left the template — and every other release — untouched.
        await SingleTemplateTask(client, "es", DistributeEs);
    }

    private async Task<ReleaseDetailDto> CreateRelease(HttpClient client, string title, DateOnly date)
    {
        var artist = (await (await client.PostAsJsonAsync("/api/artists", new ArtistInput($"{title} Artist", null)))
            .Content.ReadFromJsonAsync<ArtistDto>())!;
        var res = await client.PostAsJsonAsync("/api/releases", new ReleaseInput(
            title, ReleaseType.Single, date, artist.Id, null, null,
            new List<TrackInput> { new(null, $"{title} Song", null, null) }));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CreatedWithWarnings<ReleaseDetailDto>>())!.Data;
    }

    private static async Task<ReleaseDetailDto> GetRelease(HttpClient client, Guid id, string locale)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/releases/{id}");
        request.Headers.Add("X-Lang", locale);
        var res = await client.SendAsync(request);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ReleaseDetailDto>())!;
    }

    private static async Task<TemplateTaskDto> SingleTemplateTask(HttpClient client, string locale, string title)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/templates");
        request.Headers.Add("X-Lang", locale);
        var res = await client.SendAsync(request);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<TemplateDto>>())!
            .Single(t => t.Type == ReleaseType.Single)
            .Phases.SelectMany(p => p.Tasks)
            .Single(t => t.Title == title);
    }

    private static async Task PutTemplateTask(HttpClient client, Guid id, string title, Phase phase, string locale)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/template-tasks/{id}")
        {
            Content = JsonContent.Create(new UpdateTemplateTaskInput(title, phase)),
        };
        request.Headers.Add("X-Lang", locale);
        (await client.SendAsync(request)).EnsureSuccessStatusCode();
    }
}
