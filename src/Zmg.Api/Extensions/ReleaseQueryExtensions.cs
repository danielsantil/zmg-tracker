using Microsoft.EntityFrameworkCore;
using Zmg.Domain.Entities;

namespace Zmg.Api.Extensions;

public static class ReleaseQueryExtensions
{
    /// <summary>
    /// The full graph a <c>ReleaseDetailDto</c> needs (main artist, featured artists + their artist,
    /// tasks, tracks). Centralised so detail/create/update load an identical shape.
    /// </summary>
    public static IQueryable<Release> WithDetailIncludes(this IQueryable<Release> query) =>
        query
            .Include(r => r.MainArtist)
            .Include(r => r.FeaturedArtists).ThenInclude(fa => fa.Artist)
            .Include(r => r.Tasks)
            .Include(r => r.Tracks);
}
