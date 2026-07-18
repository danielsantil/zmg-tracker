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

    // v2.0: UPC-only (ISRC moved to the song). The warning fires only once distributed with a blank UPC.
    [Theory]
    [InlineData(false, null, false)]  // not distributed → silent
    [InlineData(true, null, true)]    // distributed, blank UPC → warn
    [InlineData(true, "   ", true)]   // distributed, whitespace UPC → warn
    [InlineData(true, "u", false)]    // distributed, UPC present → clear
    public void NeedsWarning_fires_only_when_distributed_and_upc_blank(bool distributed, string? upc, bool expected)
    {
        Assert.Equal(expected, Release.NeedsWarning(distributed, upc));
    }
}
