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

    [Fact]
    public void IsEmptyAlbumWarning_fires_for_an_active_album_with_no_tracks()
    {
        Assert.True(Release.IsEmptyAlbumWarning(ReleaseType.Album, trackCount: 0, isArchived: false));
    }

    [Fact]
    public void IsEmptyAlbumWarning_is_silent_once_a_track_is_added()
    {
        Assert.False(Release.IsEmptyAlbumWarning(ReleaseType.Album, trackCount: 1, isArchived: false));
    }

    [Fact]
    public void IsEmptyAlbumWarning_never_fires_for_a_single()
    {
        // A single always has exactly one track; the empty-album advisory is album-only.
        Assert.False(Release.IsEmptyAlbumWarning(ReleaseType.Single, trackCount: 0, isArchived: false));
    }

    [Fact]
    public void IsEmptyAlbumWarning_is_silent_for_an_archived_album()
    {
        Assert.False(Release.IsEmptyAlbumWarning(ReleaseType.Album, trackCount: 0, isArchived: true));
    }
}
