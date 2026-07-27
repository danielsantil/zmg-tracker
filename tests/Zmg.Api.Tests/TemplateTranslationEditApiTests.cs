using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Enums;
using Zmg.Infra.Data;

namespace Zmg.Api.Tests;

/// <summary>
/// M48 step 4 — correcting checklist copy in the app instead of by migration. Its own class, and so
/// its own factory/database: these tests mutate the seeded templates, which the read-only resolution
/// tests in <see cref="ChecklistTranslationApiTests"/> assert against.
/// </summary>
public class TemplateTranslationEditApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private const string MixMasterEs = "Mezcla/master";
    private const string DistributeEs = "Distribuir a los DSPs";

    /// <summary>
    /// The whole point of M48 step 4: ZMG corrects the Spanish copy in the app, not by migration. An
    /// edit lands in the locale being read, so the English standard — and the code every release task
    /// resolves through — survives untouched.
    /// </summary>
    [Fact]
    public async Task Editing_a_template_task_in_spanish_rewrites_only_the_spanish()
    {
        var client = factory.CreateClient();
        var task = await SingleTemplateTask(client, "es", DistributeEs);

        await PutTemplateTask(client, task.Id, "Distribuir a las tiendas digitales", task.Phase, "es");

        var spanish = await SingleTemplateTask(client, "es", "Distribuir a las tiendas digitales");
        Assert.Equal(task.Id, spanish.Id);
        // The album shares the code, so it picked up the same correction — the base checklist is one
        // list seeded twice, and a fix that landed on only half of it would read as a no-op.
        Assert.Contains((await GetTemplates(client, "es")).Single(t => t.Type == ReleaseType.Album)
            .Phases.SelectMany(p => p.Tasks), t => t.Title == "Distribuir a las tiendas digitales");
        // English untouched — and still resolvable, which means the code survived the edit.
        await SingleTemplateTask(client, "en", SeedData.DistributeToDspsTitle);
    }

    [Fact]
    public async Task Editing_a_template_task_in_english_leaves_the_spanish_alone()
    {
        var client = factory.CreateClient();
        var task = await SingleTemplateTask(client, "en", "Mix/master");

        await PutTemplateTask(client, task.Id, "Mix and master", task.Phase, "en");

        await SingleTemplateTask(client, "en", "Mix and master");
        await SingleTemplateTask(client, "es", MixMasterEs);
    }

    [Fact]
    public async Task Retyping_the_english_title_while_in_spanish_drops_the_translation_row()
    {
        // Text identical to the fallback is stored as *no row* — same result, less to keep in step.
        var client = factory.CreateClient();
        var task = await SingleTemplateTask(client, "es", "Pitch a Spotify");

        await PutTemplateTask(client, task.Id, "Pitch to Spotify", task.Phase, "es");

        await SingleTemplateTask(client, "es", "Pitch to Spotify");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();
        // Gone from *both* templates: translations are keyed by code, so a row left behind on the album
        // would keep feeding the old text back to the single template's task.
        Assert.False(db.TemplateTaskTranslations.Any(t => t.TemplateTask!.Code == TaskCodes.PitchSpotify));
    }

    private static async Task<TemplateTaskDto> SingleTemplateTask(HttpClient client, string locale, string title)
    {
        var templates = await GetTemplates(client, locale);
        return templates.Single(t => t.Type == ReleaseType.Single)
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

    private static async Task<List<TemplateDto>> GetTemplates(HttpClient client, string locale)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/templates");
        request.Headers.Add("X-Lang", locale);
        var res = await client.SendAsync(request);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<List<TemplateDto>>())!;
    }
}
