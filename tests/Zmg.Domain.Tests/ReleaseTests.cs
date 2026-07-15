using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

public class ReleaseTests
{
    [Fact]
    public void IsDistributed_tracks_the_distribute_task_done_state()
    {
        var release = new Release
        {
            Tasks =
            [
                new ReleaseTask
                    { Id = Guid.NewGuid(), Title = SeedData.DistributeToDspsTitle, Phase = Phase.Pre, IsDone = false }
            ]
        };
        Assert.False(release.IsDistributed);
        
        release.Tasks.Single().IsDone = true;
        Assert.True(release.IsDistributed);
    }

    [Fact]
    public void NeedsWarning_is_silent_until_distributed()
    {
        Assert.False(Release.NeedsWarning(distributed: false, upc: null));
    }

    [Fact]
    public void NeedsWarning_fires_on_a_blank_upc_once_distributed()
    {
        // v2.0: UPC-only (ISRC moved to the song).
        Assert.True(Release.NeedsWarning(distributed: true, upc: null));
        Assert.True(Release.NeedsWarning(distributed: true, upc: "   "));
    }

    [Fact]
    public void NeedsWarning_is_false_when_upc_present()
    {
        Assert.False(Release.NeedsWarning(distributed: true, upc: "u"));
    }
}
