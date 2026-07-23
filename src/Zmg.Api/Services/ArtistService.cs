using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Orchestrates artist CRUD: runs the pure <see cref="Validation"/> rules against
/// already-loaded context and persists through <see cref="ZmgDbContext"/>. Endpoints
/// stay thin and just translate the returned <see cref="OperationResult"/> to HTTP.
/// </summary>
public sealed class ArtistService(ZmgDbContext db) : IArtistService
{
    public async Task<IReadOnlyList<ArtistDto>> ListAsync(CancellationToken ct = default) =>
        await db.Artists.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(Projection)
            .ToListAsync(ct);

    public async Task<OperationResult<ArtistDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.Artists.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(Projection)
            .FirstOrDefaultAsync(ct);
        return dto is null ? OperationResult<ArtistDto>.NotFound() : OperationResult<ArtistDto>.Success(dto);
    }

    public async Task<OperationResult<ArtistDto>> CreateAsync(ArtistInput input, CancellationToken ct = default)
    {
        var others = await db.Artists.AsNoTracking().Select(a => a.Name).ToListAsync(ct);
        var validation = Validation.ValidateArtist(input.Name, others);
        if (!validation.IsValid)
            return OperationResult<ArtistDto>.Invalid(validation.Errors);

        var artist = new Artist
        {
            Id = Guid.NewGuid(),
            Name = input.Name.Trim(),
            Notes = input.Notes,
        };
        db.Artists.Add(artist);
        await db.SaveChangesAsync(ct);

        return OperationResult<ArtistDto>.Success(new ArtistDto(artist.Id, artist.Name, artist.Notes, 0, 0, 0, 0, 0));
    }

    public async Task<OperationResult<ArtistDto>> UpdateAsync(Guid id, ArtistInput input, CancellationToken ct = default)
    {
        var artist = await db.Artists.FindAsync([id], ct);
        if (artist is null) return OperationResult<ArtistDto>.NotFound();

        var others = await db.Artists.AsNoTracking().Where(a => a.Id != id).Select(a => a.Name).ToListAsync(ct);
        var validation = Validation.ValidateArtist(input.Name, others);
        if (!validation.IsValid)
            return OperationResult<ArtistDto>.Invalid(validation.Errors);

        artist.Name = input.Name.Trim();
        artist.Notes = input.Notes;
        await db.SaveChangesAsync(ct);

        // Re-project the counts in one round-trip (the projection already counts releases/songs/credits).
        var dto = await db.Artists.AsNoTracking().Where(a => a.Id == id).Select(Projection).FirstAsync(ct);
        return OperationResult<ArtistDto>.Success(dto);
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var artist = await db.Artists.FindAsync([id], ct);
        if (artist is null) return OperationResult.NotFound();

        // Only ACTIVE references block the delete: main artist of a non-archived release or song, or
        // credited on a non-archived song. (Counting *every* reference — including archived — let a
        // feat-only artist 500 on the Restrict FK; that's why the credit case is here.) Archived
        // references don't block — they get cascade-removed below, which the UI warns about first.
        var activeReleases = await db.Releases.CountAsync(r => r.MainArtistId == id && r.ArchivedAt == null, ct);
        var activeSongs = await db.Songs.CountAsync(s => s.MainArtistId == id && s.ArchivedAt == null, ct);
        var activeCredits = await db.SongArtists.CountAsync(sa => sa.ArtistId == id && sa.Song!.ArchivedAt == null, ct);
        var validation = Validation.ValidateArtistDelete(activeReleases + activeSongs + activeCredits);
        if (!validation.IsValid)
            return OperationResult.Conflict(validation.Errors);

        // No active references — hard-delete the artist, cascading away the archived data that references
        // it (all remaining references are archived by the guard above). Load the join rows we must clear
        // by hand: Track→Song is Restrict, so an archived song's links can't cascade with it. Releases and
        // credits-on-this-artist's-songs cascade at the DB. Including release tracks keeps the shared Track
        // instances single in the identity map, so nothing is deleted twice.
        var releases = await db.Releases.Include(r => r.Tracks).Where(r => r.MainArtistId == id).ToListAsync(ct);
        var songs = await db.Songs.Include(s => s.ReleaseLinks).Where(s => s.MainArtistId == id).ToListAsync(ct);
        var credits = await db.SongArtists.Where(sa => sa.ArtistId == id).ToListAsync(ct);

        foreach (var s in songs) db.Tracks.RemoveRange(s.ReleaseLinks);
        db.Releases.RemoveRange(releases);   // cascade: ReleaseTasks + Tracks
        db.Songs.RemoveRange(songs);         // cascade: SongArtist credits on these songs
        db.SongArtists.RemoveRange(credits); // this artist's own feat/collab credits elsewhere
        db.Artists.Remove(artist);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    // Shared list/detail shape: name/notes, the three ACTIVE reference counts (releases/songs/credits,
    // archived excluded), then the archived releases/songs a delete would cascade-remove.
    private static System.Linq.Expressions.Expression<Func<Artist, ArtistDto>> Projection =>
        a => new ArtistDto(
            a.Id, a.Name, a.Notes,
            a.Releases.Count(r => r.ArchivedAt == null),
            a.Songs.Count(s => s.ArchivedAt == null),
            a.SongCredits.Count(sc => sc.Song!.ArchivedAt == null),
            a.Releases.Count(r => r.ArchivedAt != null),
            a.Songs.Count(s => s.ArchivedAt != null));
}
