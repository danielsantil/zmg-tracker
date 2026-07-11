using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Data;
using Zmg.Domain;

namespace Zmg.Api.Endpoints;

public static class ArtistEndpoints
{
    public static void MapArtistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/artists");

        group.MapGet("", async (ZmgDbContext db) =>
        {
            var artists = await db.Artists
                .OrderBy(a => a.Name)
                .Select(a => new ArtistDto(a.Id, a.Name, a.Notes, a.Releases.Count))
                .ToListAsync();
            return Results.Ok(artists);
        });

        group.MapPost("", async (ArtistInput input, ZmgDbContext db) =>
        {
            var others = await db.Artists.Select(a => a.Name).ToListAsync();
            var validation = Validation.ValidateArtist(input.Name, others);
            if (!validation.IsValid)
                return Results.BadRequest(new ValidationErrorResponse(validation.Errors.ToArray()));

            var artist = new Artist
            {
                Id = Guid.NewGuid(),
                Name = input.Name.Trim(),
                Notes = input.Notes,
            };
            db.Artists.Add(artist);
            await db.SaveChangesAsync();
            return Results.Created($"/api/artists/{artist.Id}",
                new ArtistDto(artist.Id, artist.Name, artist.Notes, 0));
        });

        group.MapPut("/{id:guid}", async (Guid id, ArtistInput input, ZmgDbContext db) =>
        {
            var artist = await db.Artists.FindAsync(id);
            if (artist is null) return Results.NotFound();

            var others = await db.Artists.Where(a => a.Id != id).Select(a => a.Name).ToListAsync();
            var validation = Validation.ValidateArtist(input.Name, others);
            if (!validation.IsValid)
                return Results.BadRequest(new ValidationErrorResponse(validation.Errors.ToArray()));

            artist.Name = input.Name.Trim();
            artist.Notes = input.Notes;
            await db.SaveChangesAsync();

            var releaseCount = await db.Releases.CountAsync(r => r.MainArtistId == id);
            return Results.Ok(new ArtistDto(artist.Id, artist.Name, artist.Notes, releaseCount));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ZmgDbContext db) =>
        {
            var artist = await db.Artists.FindAsync(id);
            if (artist is null) return Results.NotFound();

            var releaseCount = await db.Releases.CountAsync(r => r.MainArtistId == id);
            var validation = Validation.ValidateArtistDelete(releaseCount);
            if (!validation.IsValid)
                return Results.Conflict(new ValidationErrorResponse(validation.Errors.ToArray()));

            db.Artists.Remove(artist);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
