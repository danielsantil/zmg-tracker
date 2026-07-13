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
        var release = new Release();
        Assert.Equal("Missing UPC, ISRC", release.MissingLabel());

        release.Upc = "u";
        Assert.Equal("Missing ISRC", release.MissingLabel());
        
        release.Isrc = "i";
        Assert.Null(release.MissingLabel());
        
        release.Upc = null;
        Assert.Equal("Missing UPC", release.MissingLabel());
    }
}
