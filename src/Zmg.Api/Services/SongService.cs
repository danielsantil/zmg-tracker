using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Catalog orchestration (M13): the song list/detail and song editing. A song's UPCs and release
/// date are derived from its links, never stored — the list surfaces the earliest non-archived
/// release date (null for orphans), and the detail exposes every linked release so the page can
/// derive the UPC list. Pure rules live in <see cref="Validation"/>; this loads/persists and maps.
/// </summary>
public sealed class SongService(ZmgDbContext db) : ISongService
{
    // scope=archived returns archived songs (ArchivedAt desc); any other scope returns active ones
    // ordered by title. Orphans (no links) are included by design. Deleted songs are hidden by the
    // global query filter. q is a case-insensitive title search.
    public async Task<IReadOnlyList<SongListItemDto>> ListAsync(string? q, string? scope)
    {
        var isArchived = string.Equals(scope, "archived", StringComparison.OrdinalIgnoreCase);

        var query = db.Songs.AsQueryable();
        query = isArchived ? query.Where(s => s.ArchivedAt != null) : query.Where(s => s.ArchivedAt == null);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s => EF.Functions.Like(s.Title, $"%{term}%"));
        }

        query = isArchived ? query.OrderByDescending(s => s.ArchivedAt) : query.OrderBy(s => s.Title);

        return await query
            .Select(s => new SongListItemDto(
                s.Id, s.Title, s.MainArtistId, s.MainArtist!.Name,
                // Earliest non-archived linked release date (deleted-release links are already hidden
                // by the Track query filter); null for orphans/unreleased.
                s.ReleaseLinks
                    .Where(t => t.Release!.ArchivedAt == null)
                    .Select(t => (DateOnly?)t.Release!.ReleaseDate)
                    .Min(),
                s.Isrc,
                s.ReleaseLinks.Count,
                s.ArchivedAt != null))
            .ToListAsync();
    }

    public async Task<OperationResult<SongDetailDto>> GetAsync(Guid id)
    {
        var song = await db.Songs
            .Include(s => s.MainArtist)
            .Include(s => s.Artists).ThenInclude(a => a.Artist)
            .Include(s => s.ReleaseLinks).ThenInclude(t => t.Release).ThenInclude(r => r!.MainArtist)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (song is null) return OperationResult<SongDetailDto>.NotFound();

        return OperationResult<SongDetailDto>.Success(ToDetail(song));
    }

    // Update: title/main-artist validated, ISRC cleaned, feat/collab artists replaced (deduped,
    // excluding the main artist). Always editable in M13 (the 409-when-archived guard is M15). A
    // rename clashing with another active song of the same main artist returns a non-blocking warning.
    public async Task<OperationResult<SongDetailDto>> UpdateAsync(Guid id, SongUpdateInput input)
    {
        var song = await db.Songs.Include(s => s.Artists).FirstOrDefaultAsync(s => s.Id == id);
        if (song is null) return OperationResult<SongDetailDto>.NotFound();

        var mainArtistExists = input.MainArtistId != Guid.Empty
            && await db.Artists.AnyAsync(a => a.Id == input.MainArtistId);
        var otherTitles = await db.Songs
            .Where(s => s.MainArtistId == input.MainArtistId && s.Id != id && s.ArchivedAt == null)
            .Select(s => s.Title)
            .ToListAsync();

        var validation = Validation.ValidateSong(input.Title, input.MainArtistId, mainArtistExists, otherTitles);
        if (!validation.IsValid)
            return OperationResult<SongDetailDto>.Invalid(validation.Errors);

        song.Title = input.Title.Trim();
        song.MainArtistId = input.MainArtistId;
        song.Isrc = string.IsNullOrWhiteSpace(input.Isrc) ? null : input.Isrc.Trim();

        song.Artists.Clear();
        foreach (var a in (input.Artists ?? new List<SongArtistInput>())
                     .Where(a => a.ArtistId != input.MainArtistId)
                     .DistinctBy(a => a.ArtistId))
        {
            song.Artists.Add(new SongArtist { SongId = song.Id, ArtistId = a.ArtistId, Role = a.Role });
        }

        await db.SaveChangesAsync();

        var updated = await db.Songs
            .Include(s => s.MainArtist)
            .Include(s => s.Artists).ThenInclude(x => x.Artist)
            .Include(s => s.ReleaseLinks).ThenInclude(t => t.Release).ThenInclude(r => r!.MainArtist)
            .FirstAsync(s => s.Id == id);
        return OperationResult<SongDetailDto>.Success(ToDetail(updated), validation.Warnings);
    }

    private static SongDetailDto ToDetail(Song song)
    {
        var artists = song.Artists
            .Select(a => new SongArtistDto(a.ArtistId, a.Artist?.Name ?? string.Empty, a.Role))
            .ToList();

        var releases = song.ReleaseLinks
            .Where(t => t.Release is not null)
            .Select(t => t.Release!)
            .OrderBy(r => r.ReleaseDate)
            .Select(r => new SongReleaseLinkDto(
                r.Id, r.Title, r.Type, r.ReleaseDate, r.Upc,
                r.MainArtistId, r.MainArtist?.Name ?? string.Empty, r.IsArchived))
            .ToList();

        return new SongDetailDto(
            song.Id, song.Title, song.MainArtistId, song.MainArtist?.Name ?? string.Empty,
            song.Isrc, song.IsArchived, artists, releases);
    }
}
