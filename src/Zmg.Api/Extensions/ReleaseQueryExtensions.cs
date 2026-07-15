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
}
