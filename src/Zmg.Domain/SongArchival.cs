using Zmg.Domain.Entities;

namespace Zmg.Domain;

/// <summary>
/// The pure rule behind the release-archive cascade (v2.0 M15). When a release is archived, each of its
/// songs is archived too — but only if the song is now dormant: not already archived, never released, and
/// with no remaining link to an active release. Released songs and songs still shared with an active
/// release stay put. No EF here, so it's unit-tested in isolation.
/// </summary>
public static class SongArchival
{
    /// <summary>
    /// Whether <paramref name="song"/> should cascade-archive when the release
    /// <paramref name="archivingReleaseId"/> is archived. True iff the song is not already archived, is
    /// upcoming (no linked release dated before <paramref name="today"/>), and every link other than the
    /// one being archived points to an already-archived release. The archiving release's own archived
    /// state is treated as archived regardless of call order (before/after its flag is stamped).
    /// </summary>
    public static bool ShouldArchive(Song song, Guid archivingReleaseId, DateOnly today)
    {
        if (song.IsArchived) return false;

        foreach (var link in song.ReleaseLinks)
        {
            var release = link.Release;
            if (release is null) continue;

            // Released anywhere (a past-dated link) → the song is out; never cascade.
            if (release.ReleaseDate < today) return false;

            // A still-active link other than the one being archived keeps the song active.
            if (link.ReleaseId != archivingReleaseId && !release.IsArchived) return false;
        }

        return true;
    }
}
