using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

public class ReleaseTests
{
    private static List<ReleaseTask> Tasks(bool distributeDone) => new()
    {
        new ReleaseTask { Id = Guid.NewGuid(), Title = "Mix/master", Phase = Phase.Pre, IsDone = true },
        new ReleaseTask { Id = Guid.NewGuid(), Title = SeedData.DistributeToDspsTitle, Phase = Phase.Pre, IsDone = distributeDone },
    };

    [Fact]
    public void IsDistributed_tracks_the_distribute_task_done_state()
    {
        Assert.False(Release.IsDistributed(Tasks(distributeDone: false)));
        Assert.True(Release.IsDistributed(Tasks(distributeDone: true)));
    }

    [Fact]
    public void NeedsWarning_is_silent_until_distributed()
    {
        Assert.False(Release.NeedsWarning(distributed: false, upc: null, isrc: null));
    }

    [Fact]
    public void NeedsWarning_fires_on_a_blank_id_once_distributed()
    {
        Assert.True(Release.NeedsWarning(distributed: true, upc: null, isrc: "x"));
        Assert.True(Release.NeedsWarning(distributed: true, upc: "x", isrc: "   "));
    }

    [Fact]
    public void NeedsWarning_is_false_when_both_ids_present()
    {
        Assert.False(Release.NeedsWarning(distributed: true, upc: "u", isrc: "i"));
    }

    [Fact]
    public void MissingLabel_lists_the_blank_ids_or_null()
    {
        Assert.Equal("Missing UPC, ISRC", Release.MissingLabel(null, null));
        Assert.Equal("Missing ISRC", Release.MissingLabel("u", null));
        Assert.Equal("Missing UPC", Release.MissingLabel(null, "i"));
        Assert.Null(Release.MissingLabel("u", "i"));
    }
}
