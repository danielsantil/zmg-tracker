using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

public class SeedDataTests
{
    // The seeded task counts are asserted end-to-end in TemplateApiTests (the integration "home"); a
    // pure change-detector here would only assert the data file says what the data file says (M25 task 11).

    [Theory]
    [InlineData("Distribute to DSPs")]
    [InlineData("Pitch to Spotify")]
    public void Single_template_pre_release_tasks_have_the_7_to_14_timeframe(string title)
    {
        // Arrange
        var single = SeedData.Templates().Single(t => t.Type == ReleaseType.Single);

        // Act
        var task = single.Tasks.Single(t => t.Title == title);

        // Assert
        Assert.Equal(7, task.MinDaysBefore);
        Assert.Equal(14, task.MaxDaysBefore);
    }

    [Fact]
    public void Seeded_task_ids_are_unique_across_both_templates()
    {
        var all = SeedData.AllTemplateTasks().ToList();
        Assert.Equal(all.Count, all.Select(t => t.Id).Distinct().Count());
    }

    // ---- Spanish checklist copy (M48) ----
    // Not change detectors: these assert *structural* properties of the translation set, which is the
    // part that breaks quietly when a code is renamed or a title is added without its Spanish.

    [Fact]
    public void Every_spanish_translation_points_at_a_real_seeded_task()
    {
        var codes = SeedData.AllTemplateTasks().Select(t => t.Id).ToHashSet();

        var translations = SeedData.AllTemplateTaskTranslations().ToList();

        Assert.NotEmpty(translations);
        Assert.All(translations, t => Assert.Contains(t.TemplateTaskId, codes));
    }

    [Fact]
    public void Spanish_rows_are_unique_per_task_and_locale_and_never_blank()
    {
        var translations = SeedData.AllTemplateTaskTranslations().ToList();

        // (TemplateTaskId, Locale) is the composite PK — a duplicate would fail the migration at deploy.
        Assert.Equal(
            translations.Count,
            translations.Select(t => (t.TemplateTaskId, t.Locale)).Distinct().Count());
        Assert.All(translations, t =>
        {
            Assert.Equal("es", t.Locale);
            Assert.False(string.IsNullOrWhiteSpace(t.Text));
        });
    }

    [Fact]
    public void A_shared_base_task_is_translated_in_both_templates()
    {
        // The base checklist is seeded into both templates as separate rows, so a title written once in
        // SpanishTitles has to land twice — otherwise albums silently read half their checklist in English.
        var byCode = SeedData.AllTemplateTasks().ToDictionary(t => t.Id, t => t.Code);
        var translated = SeedData.AllTemplateTaskTranslations()
            .Select(t => byCode[t.TemplateTaskId])
            .ToList();

        Assert.Equal(2, translated.Count(c => c == TaskCodes.DistributeToDsps));
        // An album-only task exists once, so it is translated once.
        Assert.Equal(1, translated.Count(c => c == TaskCodes.AlbumPreSave));
    }

    [Fact]
    public void The_untranslated_titles_are_the_three_deliberate_proper_nouns()
    {
        // A task with no Spanish row falls back to English by design (SeedData.SpanishTitles' remarks).
        // Pinning the set means a *forgotten* translation shows up as a failure rather than as English
        // text quietly sitting in a Spanish checklist.
        var single = SeedData.Templates().Single(t => t.Type == ReleaseType.Single);
        var translatedIds = SeedData.AllTemplateTaskTranslations().Select(t => t.TemplateTaskId).ToHashSet();

        var untranslated = single.Tasks
            .Where(t => !translatedIds.Contains(t.Id))
            .Select(t => t.Code)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
            new[] { TaskCodes.SpotifyArtistPick, TaskCodes.SpotifyCanvas, TaskCodes.SpotifyDiscoveryMode }
                .OrderBy(c => c, StringComparer.Ordinal),
            untranslated);
    }
}
