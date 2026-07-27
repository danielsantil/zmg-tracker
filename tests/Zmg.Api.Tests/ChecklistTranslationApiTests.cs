using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Domain.Enums;
using Zmg.Infra.Data;

namespace Zmg.Api.Tests;

/// <summary>
/// M47 — checklist text translated by lookup, never by rewriting rows. Everything here turns on the
/// stable <c>Code</c>/<c>SourceCode</c>: it survives translation, a rename, and the template task being
/// deleted, and it is what <c>IsDistributed</c> now keys off instead of an English title.
/// </summary>
public class ChecklistTranslationApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    private const string MixMasterEs = "Mezcla/master";
    private const string DistributeEs = "Distribuir a DSPs";

    // ---- Seed + migration ----

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
    public async Task A_release_created_now_is_stamped_with_source_codes()
    {
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Stamped Release", TestDates.Today.AddDays(20));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();
        var tasks = db.ReleaseTasks.AsNoTracking().Where(t => t.ReleaseId == release.Id).ToList();

        Assert.NotEmpty(tasks);
        Assert.All(tasks, t => Assert.False(string.IsNullOrWhiteSpace(t.SourceCode)));
        Assert.Contains(tasks, t => t.SourceCode == TaskCodes.DistributeToDsps);
    }

    /// <summary>
    /// The migration's backfill, reproduced on a row that predates it: a release task carrying only the
    /// old lineage GUID. Its release must still report as distributed — the failure mode is silent
    /// (no error, the UPC warning and the pending engine just stop firing), so it needs its own test.
    /// </summary>
    [Fact]
    public async Task A_release_whose_tasks_predate_the_code_column_still_reports_distributed()
    {
        var client = factory.CreateClient();
        var release = await CreateRelease(client, "Legacy Release", TestDates.Today.AddDays(30));
        var distributeId = release.Phases.SelectMany(p => p.Tasks)
            .Single(t => t.Title == SeedData.DistributeToDspsTitle).Id;

        // Rewind to the pre-M47 shape: SourceTemplateTaskId set, SourceCode null.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();
            var task = db.ReleaseTasks.Single(t => t.Id == distributeId);
            task.SourceCode = null;
            task.IsDone = true;
            db.SaveChanges();

            Assert.False(Distributed(db, release.Id)); // precondition: without the code it is invisible

            // What the migration's UPDATE…SELECT does, in EF terms.
            var backfilled = db.ReleaseTasks.Single(t => t.Id == distributeId);
            backfilled.SourceCode = db.TemplateTasks
                .Where(tt => tt.Id == backfilled.SourceTemplateTaskId)
                .Select(tt => tt.Code)
                .Single();
            db.SaveChanges();

            Assert.True(Distributed(db, release.Id));
        }

        // And the API agrees: distributed with a blank UPC raises the missing-UPC advisory.
        var listed = await client.GetFromJsonAsync<List<ReleaseListItemDto>>("/api/releases?scope=all");
        Assert.Contains(ReleaseWarnings.MissingUpc, listed!.Single(r => r.Id == release.Id).Warnings);

        static bool Distributed(ZmgDbContext db, Guid releaseId) =>
            db.Releases.AsNoTracking().Include(r => r.Tasks).Single(r => r.Id == releaseId).IsDistributed;
    }

    // ---- Resolution ----

    [Fact]
    public async Task Templates_answer_in_the_requested_locale_and_fall_back_to_english()
    {
        var client = factory.CreateClient();
        SeedSpanish();

        var spanish = await GetTemplates(client, "es");
        var single = spanish.Single(t => t.Type == ReleaseType.Single);
        Assert.Contains(single.Phases.SelectMany(p => p.Tasks), t => t.Title == MixMasterEs);
        // Only two codes were translated; everything else keeps its English title, never a raw slug.
        Assert.Contains(single.Phases.SelectMany(p => p.Tasks), t => t.Title == "Pitch to Spotify");

        var english = await GetTemplates(client, "en");
        Assert.Contains(english.Single(t => t.Type == ReleaseType.Single).Phases.SelectMany(p => p.Tasks),
            t => t.Title == "Mix/master");
    }

    [Fact]
    public async Task A_release_checklist_translates_through_its_source_code()
    {
        var client = factory.CreateClient();
        SeedSpanish();
        var release = await CreateRelease(client, "Locale Release", TestDates.Today.AddDays(21));

        var spanish = await GetRelease(client, release.Id, "es");
        Assert.Contains(spanish.Phases.SelectMany(p => p.Tasks), t => t.Title == DistributeEs);

        var english = await GetRelease(client, release.Id, "en");
        Assert.Contains(english.Phases.SelectMany(p => p.Tasks), t => t.Title == SeedData.DistributeToDspsTitle);
    }

    [Fact]
    public async Task An_unsupported_locale_falls_back_to_english_rather_than_failing()
    {
        var client = factory.CreateClient();
        SeedSpanish();
        var release = await CreateRelease(client, "Unsupported Locale Release", TestDates.Today.AddDays(22));

        var detail = await GetRelease(client, release.Id, "de");

        Assert.Contains(detail.Phases.SelectMany(p => p.Tasks), t => t.Title == SeedData.DistributeToDspsTitle);
    }

    [Fact]
    public async Task Accept_language_is_honoured_when_x_lang_is_absent()
    {
        var client = factory.CreateClient();
        SeedSpanish();
        var release = await CreateRelease(client, "Accept-Language Release", TestDates.Today.AddDays(23));

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/releases/{release.Id}");
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("es-MX", 0.9));
        var detail = (await (await client.SendAsync(request)).Content.ReadFromJsonAsync<ReleaseDetailDto>())!;

        Assert.Contains(detail.Phases.SelectMany(p => p.Tasks), t => t.Title == DistributeEs);
    }

    // ---- Editing drops out of translation ----

    [Fact]
    public async Task Editing_a_task_title_clears_its_source_code_and_the_text_stays_verbatim()
    {
        var client = factory.CreateClient();
        SeedSpanish();
        var release = await CreateRelease(client, "Edited Task Release", TestDates.Today.AddDays(24));
        var task = release.Phases.SelectMany(p => p.Tasks).Single(t => t.Title == "Mix/master");

        var res = await client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new UpdateTaskInput("Mezclar con Andrés", task.Phase, null));
        res.EnsureSuccessStatusCode();

        // Both languages now show the user's own text — a translation reverting their edit would be a bug.
        foreach (var locale in new[] { "en", "es" })
        {
            var detail = await GetRelease(client, release.Id, locale);
            var tasks = detail.Phases.SelectMany(p => p.Tasks).ToList();
            Assert.Contains(tasks, t => t.Title == "Mezclar con Andrés");
            Assert.DoesNotContain(tasks, t => t.Title is "Mix/master" or MixMasterEs);
        }
    }

    /// <summary>
    /// The trap in step 6: the SPA sends the whole editable row back on a phase move, so the title it
    /// echoes is the *translated* one. Comparing against the stored English column would read that as
    /// an edit and quietly overwrite the English title with Spanish for every moved task.
    /// </summary>
    [Fact]
    public async Task Moving_a_task_between_phases_in_spanish_is_not_treated_as_a_title_edit()
    {
        var client = factory.CreateClient();
        SeedSpanish();
        var release = await CreateRelease(client, "Phase Move Release", TestDates.Today.AddDays(25));
        var task = release.Phases.SelectMany(p => p.Tasks).Single(t => t.Title == SeedData.DistributeToDspsTitle);

        // Read it in Spanish, then send that Spanish title straight back with a new phase.
        var spanish = await GetRelease(client, release.Id, "es");
        var asShown = spanish.Phases.SelectMany(p => p.Tasks).Single(t => t.Id == task.Id);
        Assert.Equal(DistributeEs, asShown.Title);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tasks/{task.Id}")
        {
            Content = JsonContent.Create(new UpdateTaskInput(asShown.Title, Phase.Release, null)),
        };
        request.Headers.Add("X-Lang", "es");
        (await client.SendAsync(request)).EnsureSuccessStatusCode();

        var english = await GetRelease(client, release.Id, "en");
        var moved = english.Phases.SelectMany(p => p.Tasks).Single(t => t.Id == task.Id);
        Assert.Equal(SeedData.DistributeToDspsTitle, moved.Title); // English intact
        Assert.Equal(Phase.Release, moved.Phase);                  // and the move landed
    }

    // ---- Helpers ----

    /// <summary>Two codes only — enough to prove resolution while leaving the fallback path exercised.</summary>
    private void SeedSpanish()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZmgDbContext>();
        if (db.TemplateTaskTranslations.Any()) return;

        var wanted = new Dictionary<string, string>
        {
            [TaskCodes.MixMaster] = MixMasterEs,
            [TaskCodes.DistributeToDsps] = DistributeEs,
        };

        // One row per template task, so the base checklist's two copies (single + album) both translate.
        foreach (var task in db.TemplateTasks.AsNoTracking().Where(t => wanted.Keys.Contains(t.Code!)).ToList())
        {
            db.TemplateTaskTranslations.Add(new TemplateTaskTranslation
            {
                TemplateTaskId = task.Id,
                Locale = "es",
                Text = wanted[task.Code!],
            });
        }
        db.SaveChanges();
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

    private static async Task<ReleaseDetailDto> GetRelease(HttpClient client, Guid id, string locale) =>
        (await Send<ReleaseDetailDto>(client, $"/api/releases/{id}", locale))!;

    private static async Task<List<TemplateDto>> GetTemplates(HttpClient client, string locale) =>
        (await Send<List<TemplateDto>>(client, "/api/templates", locale))!;

    private static async Task<T?> Send<T>(HttpClient client, string path, string locale)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Lang", locale);
        var res = await client.SendAsync(request);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<T>();
    }
}
