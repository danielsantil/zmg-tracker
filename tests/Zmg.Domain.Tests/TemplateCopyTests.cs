using Zmg.Domain;

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
        Assert.Equal(new[] { "Mix/master", "Design cover" }, pre.Select(t => t.Title));
        Assert.Equal(new[] { 0, 1 }, pre.Select(t => t.SortOrder));
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
}

public class SeedDataTests
{
    [Fact]
    public void Single_template_has_the_exact_section_5_4_counts()
    {
        var single = SeedData.Templates().Single(t => t.Type == ReleaseType.Single);

        Assert.Equal(5, single.Tasks.Count(t => t.Phase == Phase.Pre));
        Assert.Equal(18, single.Tasks.Count(t => t.Phase == Phase.Release));
        Assert.Equal(7, single.Tasks.Count(t => t.Phase == Phase.Post));
        Assert.Equal(30, single.Tasks.Count);
    }

    [Fact]
    public void Album_template_is_the_single_list_plus_album_extras()
    {
        var album = SeedData.Templates().Single(t => t.Type == ReleaseType.Album);

        Assert.Equal(40, album.Tasks.Count);
        Assert.Contains(album.Tasks, t => t.Title.Contains("Finalize tracklist"));
        Assert.Contains(album.Tasks, t => t.Title.Contains("waterfall"));
    }

    [Fact]
    public void Seeded_task_ids_are_unique_across_both_templates()
    {
        var all = SeedData.AllTemplateTasks().ToList();
        Assert.Equal(all.Count, all.Select(t => t.Id).Distinct().Count());
    }
}
