using Zmg.Domain;

namespace Zmg.Domain.Tests;

public class IdentifierStateTests
{
    private static List<ReleaseTask> Tasks(bool distributeDone) => new()
    {
        new ReleaseTask { Id = Guid.NewGuid(), Title = "Mix/master", Phase = Phase.Pre, IsDone = true },
        new ReleaseTask { Id = Guid.NewGuid(), Title = SeedData.DistributeToDspsTitle, Phase = Phase.Pre, IsDone = distributeDone },
    };

    [Fact]
    public void IsDistributed_tracks_the_distribute_task_done_state()
    {
        Assert.False(IdentifierState.IsDistributed(Tasks(distributeDone: false)));
        Assert.True(IdentifierState.IsDistributed(Tasks(distributeDone: true)));
    }

    [Fact]
    public void NeedsWarning_is_silent_until_distributed()
    {
        Assert.False(IdentifierState.NeedsWarning(distributed: false, upc: null, isrc: null));
    }

    [Fact]
    public void NeedsWarning_fires_on_a_blank_id_once_distributed()
    {
        Assert.True(IdentifierState.NeedsWarning(distributed: true, upc: null, isrc: "x"));
        Assert.True(IdentifierState.NeedsWarning(distributed: true, upc: "x", isrc: "   "));
    }

    [Fact]
    public void NeedsWarning_is_false_when_both_ids_present()
    {
        Assert.False(IdentifierState.NeedsWarning(distributed: true, upc: "u", isrc: "i"));
    }

    [Fact]
    public void MissingLabel_lists_the_blank_ids_or_null()
    {
        Assert.Equal("Missing UPC, ISRC", IdentifierState.MissingLabel(null, null));
        Assert.Equal("Missing ISRC", IdentifierState.MissingLabel("u", null));
        Assert.Equal("Missing UPC", IdentifierState.MissingLabel(null, "i"));
        Assert.Null(IdentifierState.MissingLabel("u", "i"));
    }
}
