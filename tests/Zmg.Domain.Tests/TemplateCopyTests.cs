using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

public class TemplateCopyTests
{
    private static ChecklistTemplate SampleTemplate() => new()
    {
        Id = Guid.NewGuid(),
        Type = ReleaseType.Single,
        Tasks =
        {
            new TemplateTask { Id = Guid.NewGuid(), Title = "Mix/master", Phase = Phase.Pre, SortOrder = 0 },
            new TemplateTask { Id = Guid.NewGuid(), Title = "Design cover", Phase = Phase.Pre, SortOrder = 1 },
            new TemplateTask { Id = Guid.NewGuid(), Title = "Distribute to DSPs", Phase = Phase.Pre, SortOrder = 2, MinDaysBefore = 7, MaxDaysBefore = 14 },
            new TemplateTask { Id = Guid.NewGuid(), Title = "Smart link", Phase = Phase.Release, SortOrder = 0 },
            new TemplateTask { Id = Guid.NewGuid(), Title = "Meta ads", Phase = Phase.Post, SortOrder = 0 },
        }
    };

    [Fact]
    public void CopyToRelease_copies_every_task()
    {
        var template = SampleTemplate();
        var releaseId = Guid.NewGuid();

        var tasks = TemplateCopy.CopyToRelease(template, releaseId);

        Assert.Equal(template.Tasks.Count, tasks.Count);
        Assert.All(tasks, t => Assert.Equal(releaseId, t.ReleaseId));
    }

    [Fact]
    public void CopyToRelease_preserves_title_phase_and_order()
    {
        var template = SampleTemplate();

        var tasks = TemplateCopy.CopyToRelease(template, Guid.NewGuid());

        var pre = tasks.Where(t => t.Phase == Phase.Pre).OrderBy(t => t.SortOrder).ToList();
        Assert.Equal(new[] { "Mix/master", "Design cover", "Distribute to DSPs" }, pre.Select(t => t.Title));
        Assert.Equal(new[] { 0, 1, 2 }, pre.Select(t => t.SortOrder));
    }

    [Fact]
    public void CopyToRelease_starts_all_tasks_incomplete()
    {
        var tasks = TemplateCopy.CopyToRelease(SampleTemplate(), Guid.NewGuid());

        Assert.All(tasks, t =>
        {
            Assert.False(t.IsDone);
            Assert.Null(t.CompletedAt);
        });
    }

    [Fact]
    public void CopyToRelease_records_lineage_to_template_task()
    {
        var template = SampleTemplate();
        var sourceIds = template.Tasks.Select(t => t.Id).ToHashSet();

        var tasks = TemplateCopy.CopyToRelease(template, Guid.NewGuid());

        Assert.All(tasks, t => Assert.Contains(t.SourceTemplateTaskId!.Value, sourceIds));
    }

    [Fact]
    public void CopyToRelease_gives_each_task_a_fresh_id()
    {
        var template = SampleTemplate();

        var tasks = TemplateCopy.CopyToRelease(template, Guid.NewGuid());

        // New ids, distinct, and not reusing the template task ids.
        Assert.Equal(tasks.Count, tasks.Select(t => t.Id).Distinct().Count());
        Assert.Empty(tasks.Select(t => t.Id).Intersect(template.Tasks.Select(t => t.Id)));
    }

    [Fact]
    public void CopyToRelease_carries_the_task_timeframe()
    {
        var template = SampleTemplate();

        var tasks = TemplateCopy.CopyToRelease(template, Guid.NewGuid());

        var distribute = tasks.Single(t => t.Title == "Distribute to DSPs");
        Assert.Equal(7, distribute.MinDaysBefore);
        Assert.Equal(14, distribute.MaxDaysBefore);

        // Tasks without a timeframe stay null.
        var mix = tasks.Single(t => t.Title == "Mix/master");
        Assert.Null(mix.MinDaysBefore);
        Assert.Null(mix.MaxDaysBefore);
    }
}

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
    public void Single_template_distribute_to_dsps_is_the_third_pre_task_at_7_to_14()
    {
        var single = SeedData.Templates().Single(t => t.Type == ReleaseType.Single);
        var pre = single.Tasks.Where(t => t.Phase == Phase.Pre).OrderBy(t => t.SortOrder).ToList();

        var distribute = pre[2];
        Assert.Equal("Distribute to DSPs", distribute.Title);
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
