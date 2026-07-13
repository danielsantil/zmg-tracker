using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

public class SeedDataTests
{
    [Fact]
    public void Single_template_has_the_v1_1_counts()
    {
        var single = SeedData.Templates().Single(t => t.Type == ReleaseType.Single);

        // v1.1 adds "Distribute to DSPs" as a 6th Pre task: 30 → 31 (6 Pre / 18 Release / 7 Post).
        Assert.Equal(6, single.Tasks.Count(t => t.Phase == Phase.Pre));
        Assert.Equal(18, single.Tasks.Count(t => t.Phase == Phase.Release));
        Assert.Equal(7, single.Tasks.Count(t => t.Phase == Phase.Post));
        Assert.Equal(31, single.Tasks.Count);
    }

    [Fact]
    public void Single_template_distribute_to_dsps_has_the_7_to_14_timeframe()
    {
        var single = SeedData.Templates().Single(t => t.Type == ReleaseType.Single);
        var distribute = single.Tasks.Single(t => t.Title == "Distribute to DSPs");

        Assert.Equal(7, distribute.MinDaysBefore);
        Assert.Equal(14, distribute.MaxDaysBefore);
    }

    [Fact]
    public void Single_template_pitch_to_spotify_has_the_7_to_14_timeframe()
    {
        var single = SeedData.Templates().Single(t => t.Type == ReleaseType.Single);
        var spotify = single.Tasks.Single(t => t.Title == "Pitch to Spotify");

        Assert.Equal(7, spotify.MinDaysBefore);
        Assert.Equal(14, spotify.MaxDaysBefore);
    }

    [Fact]
    public void Seeded_task_ids_are_unique_across_both_templates()
    {
        var all = SeedData.AllTemplateTasks().ToList();
        Assert.Equal(all.Count, all.Select(t => t.Id).Distinct().Count());
    }
}