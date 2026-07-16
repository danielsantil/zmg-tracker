using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Domain.Tests;

/// <summary>The soft release advisories, returned as one flat label list (Missing UPC + album nags).</summary>
public class ReleaseWarningsTests
{
    private static List<string> Compute(
        ReleaseType type, int trackCount, bool isArchived = false, bool distributed = false, string? upc = null) =>
        ReleaseWarnings.Compute(type, trackCount, isArchived, distributed, upc);

    [Fact]
    public void No_warnings_for_a_healthy_single()
    {
        Assert.Empty(Compute(ReleaseType.Single, trackCount: 1, distributed: true, upc: "u"));
    }

    [Fact]
    public void Missing_upc_fires_only_once_distributed_with_a_blank_upc()
    {
        Assert.DoesNotContain(ReleaseWarnings.MissingUpc, Compute(ReleaseType.Single, 1, distributed: false, upc: null));
        Assert.Contains(ReleaseWarnings.MissingUpc, Compute(ReleaseType.Single, 1, distributed: true, upc: null));
        Assert.DoesNotContain(ReleaseWarnings.MissingUpc, Compute(ReleaseType.Single, 1, distributed: true, upc: "u"));
    }

    [Fact]
    public void Empty_album_and_one_track_album_are_mutually_exclusive()
    {
        Assert.Equal(new[] { ReleaseWarnings.AlbumIsEmpty }, Compute(ReleaseType.Album, trackCount: 0));
        Assert.Equal(new[] { ReleaseWarnings.AlbumHasOneTrack }, Compute(ReleaseType.Album, trackCount: 1));
        Assert.Empty(Compute(ReleaseType.Album, trackCount: 2));
    }

    [Fact]
    public void Album_nags_never_apply_to_a_single()
    {
        Assert.Empty(Compute(ReleaseType.Single, trackCount: 0));
    }

    [Fact]
    public void A_distributed_empty_album_with_no_upc_reports_both_warnings()
    {
        var warnings = Compute(ReleaseType.Album, trackCount: 0, distributed: true, upc: null);
        Assert.Contains(ReleaseWarnings.MissingUpc, warnings);
        Assert.Contains(ReleaseWarnings.AlbumIsEmpty, warnings);
    }

    [Fact]
    public void Archived_releases_report_no_warnings()
    {
        // Would otherwise report both, but archived releases are terminal/read-only.
        Assert.Empty(Compute(ReleaseType.Album, trackCount: 0, isArchived: true, distributed: true, upc: null));
    }
}
