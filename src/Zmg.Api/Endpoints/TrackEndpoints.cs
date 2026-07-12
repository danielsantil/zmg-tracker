using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Data;
using Zmg.Domain;

namespace Zmg.Api.Endpoints;

/// <summary>
/// Album track list (M4). Tracks belong to a release (albums in practice; the UI only
/// surfaces the list for album releases). Ordering is by <see cref="Track.TrackNumber"/>,
/// which stays 1-based and contiguous; reorder rewrites it from the given id order.
/// </summary>
public static class TrackEndpoints
{
    public static void MapTrackEndpoints(this IEndpointRouteBuilder app)
    {
        // Add a track to a release, appended after the current last track.
        app.MapPost("/api/releases/{releaseId:guid}/tracks", async (Guid releaseId, AddTrackInput input, ZmgDbContext db) =>
        {
            if (!await db.Releases.AnyAsync(r => r.Id == releaseId))
                return Results.NotFound();

            var validation = Validation.ValidateTrackTitle(input.Title);
            if (!validation.IsValid)
                return Results.BadRequest(new ValidationErrorResponse(validation.Errors.ToArray()));

            var lastNumber = await db.Tracks
                .Where(t => t.ReleaseId == releaseId)
                .Select(t => (int?)t.TrackNumber)
                .MaxAsync() ?? 0;

            var track = new Track
            {
                Id = Guid.NewGuid(),
                ReleaseId = releaseId,
                TrackNumber = lastNumber + 1,
                Title = input.Title.Trim(),
                IsFocusTrack = false,
            };
            db.Tracks.Add(track);
            await db.SaveChangesAsync();

            return Results.Created($"/api/tracks/{track.Id}", ToDto(track));
        });

        // Rename and/or set the focus-track flag.
        app.MapPut("/api/tracks/{id:guid}", async (Guid id, UpdateTrackInput input, ZmgDbContext db) =>
        {
            var track = await db.Tracks.FindAsync(id);
            if (track is null) return Results.NotFound();

            var validation = Validation.ValidateTrackTitle(input.Title);
            if (!validation.IsValid)
                return Results.BadRequest(new ValidationErrorResponse(validation.Errors.ToArray()));

            track.Title = input.Title.Trim();
            track.IsFocusTrack = input.IsFocusTrack;
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(track));
        });

        // Toggle the focus-track flag (the quick daily action).
        app.MapPatch("/api/tracks/{id:guid}/focus", async (Guid id, ZmgDbContext db) =>
        {
            var track = await db.Tracks.FindAsync(id);
            if (track is null) return Results.NotFound();

            track.IsFocusTrack = !track.IsFocusTrack;
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(track));
        });

        // Reorder tracks; TrackNumber follows the given id order (1-based).
        app.MapPut("/api/releases/{releaseId:guid}/tracks/order", async (Guid releaseId, ReorderTracksInput input, ZmgDbContext db) =>
        {
            var tracks = await db.Tracks
                .Where(t => t.ReleaseId == releaseId)
                .ToListAsync();
            if (tracks.Count == 0) return Results.NotFound();

            var byId = tracks.ToDictionary(t => t.Id);
            // Every track on the release must appear exactly once in the request.
            if (input.OrderedTrackIds.Count != tracks.Count || input.OrderedTrackIds.Any(tid => !byId.ContainsKey(tid)))
                return Results.BadRequest(new ValidationErrorResponse(
                    new[] { "Reorder must list every track on the release exactly once." }));

            for (var i = 0; i < input.OrderedTrackIds.Count; i++)
                byId[input.OrderedTrackIds[i]].TrackNumber = i + 1;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // Delete a track and close the gap in numbering.
        app.MapDelete("/api/tracks/{id:guid}", async (Guid id, ZmgDbContext db) =>
        {
            var track = await db.Tracks.FindAsync(id);
            if (track is null) return Results.NotFound();

            db.Tracks.Remove(track);
            // Renumber the survivors so TrackNumber stays contiguous.
            var rest = await db.Tracks
                .Where(t => t.ReleaseId == track.ReleaseId && t.Id != id)
                .OrderBy(t => t.TrackNumber)
                .ToListAsync();
            for (var i = 0; i < rest.Count; i++)
                rest[i].TrackNumber = i + 1;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static TrackDto ToDto(Track t) =>
        new(t.Id, t.TrackNumber, t.Title, t.IsFocusTrack);
}
