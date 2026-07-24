using Microsoft.EntityFrameworkCore;
using Zmg.Domain.Entities;

namespace Zmg.Api.Extensions;

public static class SongQueryExtensions
{
    /// <summary>
    /// The full graph a <c>SongDetailDto</c> needs (main artist, feat/collab artists, and each
    /// non-archived linked release with its main artist). Centralised so detail/create/update load an
    /// identical shape — mirrors <see cref="ReleaseQueryExtensions.WithDetailIncludes"/> (M25 task 3).
    /// </summary>
    public static IQueryable<Song> WithDetailIncludes(this IQueryable<Song> query) =>
        query
            .Include(s => s.MainArtist)
            .Include(s => s.Artists).ThenInclude(a => a.Artist)
            .Include(s => s.ReleaseLinks)
            .ThenInclude(t => t.Release).ThenInclude(r => r!.MainArtist);
}
