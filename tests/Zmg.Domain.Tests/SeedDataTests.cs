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
}