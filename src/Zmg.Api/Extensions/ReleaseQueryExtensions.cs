using Microsoft.EntityFrameworkCore;
using Zmg.Domain.Entities;

namespace Zmg.Api.Extensions;

public static class ReleaseQueryExtensions
{
    /// <summary>
    /// The full graph a <c>ReleaseDetailDto</c> needs (main artist, tasks, tracks and each track's
    /// song with its feat/collab artists). Centralised so detail/create/update load an identical shape.
    /// </summary>
    public static IQueryable<Release> WithDetailIncludes(this IQueryable<Release> query) =>
        query
            .Include(r => r.MainArtist)
            .Include(r => r.Tasks)
            .Include(r => r.Tracks).ThenInclude(t => t.Song!).ThenInclude(s => s.Artists).ThenInclude(sa => sa.Artist);

    /// <summary>
    /// The graph the archive cascade walks: each track's song, and every release that song is still
    /// linked to (<c>SongArchival.ShouldArchive</c> needs the other links to spot a shared song).
    /// Shared by the archive itself and its read-only preview so the two can never diverge.
    /// </summary>
    public static IQueryable<Release> WithArchiveCascadeIncludes(this IQueryable<Release> query) =>
        query
            .Include(r => r.Tracks).ThenInclude(t => t.Song).ThenInclude(s => s!.ReleaseLinks).ThenInclude(t => t.Release);
}
