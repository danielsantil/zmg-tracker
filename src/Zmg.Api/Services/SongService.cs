using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
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
    // ordered by title. Orphans (no links) are included by design. q is a case-insensitive title search.
    public async Task<IReadOnlyList<SongListItemDto>> ListAsync(string? q, string? scope, Guid? artistId, CancellationToken ct = default)
    {
        var isArchived = string.Equals(scope, "archived", StringComparison.OrdinalIgnoreCase);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = db.Songs.AsNoTracking();
        query = isArchived ? query.Where(s => s.ArchivedAt != null) : query.Where(s => s.ArchivedAt == null);
        if (artistId is { } aid) query = query.Where(s => s.MainArtistId == aid);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s => EF.Functions.Like(s.Title.ToLower(), $"%{term.ToLower()}%"));
        }

        query = isArchived ? query.OrderByDescending(s => s.ArchivedAt) : query.OrderBy(s => s.Title);

        return await query
            .Select(s => new SongListItemDto(
                s.Id, s.Title, s.MainArtistId, s.MainArtist!.Name,
                // Earliest non-archived linked release date; null for orphans/unreleased.
                s.ReleaseLinks
                    .Where(t => t.Release!.ArchivedAt == null)
                    .Select(t => (DateOnly?)t.Release!.ReleaseDate)
                    .Min(),
                s.Isrc,
                s.ReleaseLinks.Count,
                s.ArchivedAt != null,
                // CanArchive (M15): an active, non-orphan song that manual-archive would accept — no link
                // to an active (non-archived) release and none released (past-dated). Orphans get Delete.
                s.ArchivedAt == null
                    && s.ReleaseLinks.Any()
                    && !s.ReleaseLinks.Any(t => t.Release!.ArchivedAt == null)
                    && !s.ReleaseLinks.Any(t => t.Release!.ReleaseDate < today),
                // IsOrphan (M15): no (non-deleted) release links.
                !s.ReleaseLinks.Any()))
            .ToListAsync(ct);
    }

    public async Task<OperationResult<SongDetailDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var song = await db.Songs.AsNoTracking().WithDetailIncludes().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (song is null) return OperationResult<SongDetailDto>.NotFound();

        return OperationResult<SongDetailDto>.Success(ToDetail(song));
    }

    // Create a catalog song directly (M2.0 improvement). Same rules as an inline release song, minus
    // the release: title/main-artist validated, ISRC cleaned, feats/collabs deduped. Born an orphan
    // (no release links). A title clashing with another active same-artist song is a hard error.
    public async Task<OperationResult<SongDetailDto>> CreateAsync(SongCreateInput input, CancellationToken ct = default)
    {
        var mainArtistExists = input.MainArtistId != Guid.Empty
            && await db.Artists.AsNoTracking().AnyAsync(a => a.Id == input.MainArtistId, ct);
        var otherTitles = await db.Songs.AsNoTracking()
            .Where(s => s.MainArtistId == input.MainArtistId && s.ArchivedAt == null)
            .Select(s => s.Title)
            .ToListAsync(ct);

        var validation = Validation.ValidateSong(input.Title, input.MainArtistId, mainArtistExists, otherTitles);
        if (!validation.IsValid)
            return OperationResult<SongDetailDto>.Invalid(validation.Errors);

        var song = SongMapping.NewSong(input.MainArtistId, input.Title, input.Isrc, input.Artists);
        db.Songs.Add(song);
        await db.SaveChangesAsync(ct);

        // Re-query with the full graph: the new SongArtist rows carry no Artist navigation, and MainArtist
        // isn't loaded, so mapping needs the names the includes bring in.
        var created = await db.Songs.AsNoTracking().WithDetailIncludes().FirstAsync(s => s.Id == song.Id, ct);
        return OperationResult<SongDetailDto>.Success(ToDetail(created), validation.Warnings);
    }

    // Update: title/main-artist validated, ISRC cleaned, feat/collab artists replaced (deduped,
    // excluding the main artist). A rename clashing with another active song of the same main artist is
    // a hard error. Archived songs are read-only (M15).
    public async Task<OperationResult<SongDetailDto>> UpdateAsync(Guid id, SongUpdateInput input, CancellationToken ct = default)
    {
        var song = await db.Songs.Include(s => s.Artists).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (song is null) return OperationResult<SongDetailDto>.NotFound();
        if (song.IsArchived)
            return OperationResult<SongDetailDto>.Conflict(new[] { "Archived songs are read-only." });

        // Main artist is immutable after creation: the song may already be on releases under the
        // original artist, so re-pointing it would create cross-artist data inconsistency.
        if (input.MainArtistId != song.MainArtistId)
            return OperationResult<SongDetailDto>.Conflict(new[] { "A song's main artist can't be changed after creation." });

        var otherTitles = await db.Songs.AsNoTracking()
            .Where(s => s.MainArtistId == song.MainArtistId && s.Id != id && s.ArchivedAt == null)
            .Select(s => s.Title)
            .ToListAsync(ct);

        var validation = Validation.ValidateSong(input.Title, song.MainArtistId, mainArtistExists: true, otherTitles);
        if (!validation.IsValid)
            return OperationResult<SongDetailDto>.Invalid(validation.Errors);

        song.Title = input.Title.Trim();
        song.Isrc = string.IsNullOrWhiteSpace(input.Isrc) ? null : input.Isrc.Trim();

        song.Artists.Clear();
        foreach (var a in (input.Artists ?? new List<SongArtistInput>())
                     .Where(a => a.ArtistId != song.MainArtistId)
                     .DistinctBy(a => a.ArtistId))
        {
            song.Artists.Add(new SongArtist { SongId = song.Id, ArtistId = a.ArtistId, Role = a.Role });
        }

        await db.SaveChangesAsync(ct);

        var updated = await db.Songs.AsNoTracking().WithDetailIncludes().FirstAsync(s => s.Id == id, ct);
        return OperationResult<SongDetailDto>.Success(ToDetail(updated), validation.Warnings);
    }

    // Manual archive (M15): a terminal, non-restorable state — mirrors the release lifecycle. In practice
    // this applies mostly to orphans; songs on active releases archive via the release-archive cascade.
    public async Task<OperationResult> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var song = await db.Songs
            .Include(s => s.ReleaseLinks).ThenInclude(t => t.Release)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (song is null) return OperationResult.NotFound();
        if (song.IsArchived) return OperationResult.Conflict(new[] { "Song is already archived." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (song.ReleaseLinks.Any(t => t.Release is { ArchivedAt: null }))
            return OperationResult.Conflict(new[] { "Song is on an active release — archive flows through the release." });
        if (song.ReleaseLinks.Any(t => t.Release is not null && t.Release.ReleaseDate < today))
            return OperationResult.Conflict(new[] { "A released song can't be archived." });

        song.ArchivedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    // Delete (M15, hard-delete M36): allowed only for an archived song or an orphan (never released). The
    // Track FK to Song is Restrict, so any lingering links (an archived-but-linked song) are removed in the
    // same SaveChanges; orphans have none. Feats/collabs cascade with the song.
    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var song = await db.Songs
            .Include(s => s.ReleaseLinks)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (song is null) return OperationResult.NotFound();

        var isOrphan = song.ReleaseLinks.Count == 0;
        if (!song.IsArchived && !isOrphan)
            return OperationResult.Conflict(new[] { "Only archived or never-released songs can be removed." });

        db.Tracks.RemoveRange(song.ReleaseLinks);
        db.Songs.Remove(song);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
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
