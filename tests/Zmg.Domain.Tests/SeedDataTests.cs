using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

public class SeedDataTests
{
    // The seeded task counts are asserted end-to-end in TemplateApiTests (the integration "home"); a
    // pure change-detector here would only assert the data file says what the data file says (M25 task 11).

    [Theory]
    [InlineData(TaskCodes.DistributeToDsps)]
    [InlineData(TaskCodes.PitchSpotify)]
    public void Single_template_pre_release_tasks_have_the_7_to_14_timeframe(string code)
    {
        // Arrange — located by code, never by title: a title is display copy in two languages that the
        // user may reword at any time, which is the whole point of v2.9.
        var single = SeedData.Templates().Single(t => t.Type == ReleaseType.Single);

        // Act
        var task = single.Tasks.Single(t => t.Code == code);

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

    // ---- Checklist copy (v2.9) ----
    // Not change detectors: these assert *structural* properties of the seeded text, which is the part
    // that breaks quietly when a task is added without its Spanish or a code is renamed.

    [Fact]
    public void Every_seeded_task_carries_both_languages()
    {
        // A null TitleEs is a state the schema supports — it means "reads the same in both languages",
        // and the templates editor can produce one. No *seeded* task uses it, though, so a translation
        // forgotten during a future edit fails here instead of sitting quietly as English text inside a
        // Spanish checklist.
        var all = SeedData.AllTemplateTasks().ToList();

        Assert.All(all, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.TitleEn));
            Assert.False(string.IsNullOrWhiteSpace(t.TitleEs));
        });
    }

    [Fact]
    public void Every_seeded_task_has_a_code()
    {
        // Null codes are for user-added tasks only. A seeded task without one is invisible to any rule
        // that identifies it — which is exactly how the IsDistributed bug went unnoticed.
        Assert.All(SeedData.AllTemplateTasks(), t => Assert.False(string.IsNullOrWhiteSpace(t.Code)));
    }

    [Fact]
    public void A_shared_base_task_is_seeded_into_both_templates_with_the_same_text()
    {
        // The base checklist is seeded into both templates as separate rows (v2.9 keeps template edits
        // per-template, so they diverge only when the user edits one). At seed time they must match, or
        // albums start life reading a different checklist from singles.
        var byCode = SeedData.AllTemplateTasks()
            .Where(t => t.Code == TaskCodes.DistributeToDsps)
            .ToList();

        Assert.Equal(2, byCode.Count);
        Assert.Single(byCode.Select(t => t.TitleEn).Distinct());
        Assert.Single(byCode.Select(t => t.TitleEs).Distinct());

        // An album-only task exists exactly once.
        Assert.Single(SeedData.AllTemplateTasks().Where(t => t.Code == TaskCodes.AlbumPreSave));
    }

    [Fact]
    public void Codes_are_unique_within_each_template()
    {
        foreach (var template in SeedData.Templates())
        {
            var codes = template.Tasks.Select(t => t.Code).ToList();
            Assert.Equal(codes.Count, codes.Distinct().Count());
        }
    }
}
