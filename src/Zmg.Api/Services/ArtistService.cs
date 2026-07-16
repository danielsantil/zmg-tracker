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
    public async Task<IReadOnlyList<ArtistDto>> ListAsync() =>
        await db.Artists
            .OrderBy(a => a.Name)
            .Select(a => new ArtistDto(a.Id, a.Name, a.Notes, a.Releases.Count, a.Songs.Count, a.SongCredits.Count))
            .ToListAsync();

    public async Task<OperationResult<ArtistDto>> GetAsync(Guid id)
    {
        var dto = await db.Artists
            .Where(a => a.Id == id)
            .Select(a => new ArtistDto(a.Id, a.Name, a.Notes, a.Releases.Count, a.Songs.Count, a.SongCredits.Count))
            .FirstOrDefaultAsync();
        return dto is null ? OperationResult<ArtistDto>.NotFound() : OperationResult<ArtistDto>.Success(dto);
    }

    public async Task<OperationResult<ArtistDto>> CreateAsync(ArtistInput input)
    {
        var others = await db.Artists.Select(a => a.Name).ToListAsync();
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
        await db.SaveChangesAsync();

        return OperationResult<ArtistDto>.Success(new ArtistDto(artist.Id, artist.Name, artist.Notes, 0, 0, 0));
    }

    public async Task<OperationResult<ArtistDto>> UpdateAsync(Guid id, ArtistInput input)
    {
        var artist = await db.Artists.FindAsync(id);
        if (artist is null) return OperationResult<ArtistDto>.NotFound();

        var others = await db.Artists.Where(a => a.Id != id).Select(a => a.Name).ToListAsync();
        var validation = Validation.ValidateArtist(input.Name, others);
        if (!validation.IsValid)
            return OperationResult<ArtistDto>.Invalid(validation.Errors);

        artist.Name = input.Name.Trim();
        artist.Notes = input.Notes;
        await db.SaveChangesAsync();

        var releaseCount = await db.Releases.CountAsync(r => r.MainArtistId == id);
        var songCount = await db.Songs.CountAsync(s => s.MainArtistId == id);
        var creditCount = await db.SongArtists.CountAsync(sa => sa.ArtistId == id);
        return OperationResult<ArtistDto>.Success(new ArtistDto(artist.Id, artist.Name, artist.Notes, releaseCount, songCount, creditCount));
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        var artist = await db.Artists.FindAsync(id);
        if (artist is null) return OperationResult.NotFound();

        // Blocked if the artist is referenced by any Restrict FK: main artist of a release or song,
        // or credited (feat/collab) on a song via SongArtist. Missing the credit case here let a
        // feat-only artist slip past the guard and 500 on the FK instead of a clean conflict.
        var releaseCount = await db.Releases.CountAsync(r => r.MainArtistId == id);
        var songCount = await db.Songs.CountAsync(s => s.MainArtistId == id);
        var creditCount = await db.SongArtists.CountAsync(sa => sa.ArtistId == id);
        var validation = Validation.ValidateArtistDelete(releaseCount + songCount + creditCount);
        if (!validation.IsValid)
            return OperationResult.Conflict(validation.Errors);

        db.Artists.Remove(artist);
        await db.SaveChangesAsync();
        return OperationResult.Success();
    }
}
