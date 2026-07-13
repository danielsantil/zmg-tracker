using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Album track list (M4). TrackNumber stays 1-based and contiguous; add appends, delete
/// closes the gap, reorder rewrites it from the given id order.
/// </summary>
public sealed class TrackService(ZmgDbContext db) : ITrackService
{
    public async Task<OperationResult<TrackDto>> AddAsync(Guid releaseId, AddTrackInput input)
    {
        if (!await db.Releases.AnyAsync(r => r.Id == releaseId))
            return OperationResult<TrackDto>.NotFound();

        var validation = Validation.ValidateTrackTitle(input.Title);
        if (!validation.IsValid)
            return OperationResult<TrackDto>.Invalid(validation.Errors);

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

        return OperationResult<TrackDto>.Success(ToDto(track));
    }

    public async Task<OperationResult<TrackDto>> UpdateAsync(Guid id, UpdateTrackInput input)
    {
        var track = await db.Tracks.FindAsync(id);
        if (track is null) return OperationResult<TrackDto>.NotFound();

        var validation = Validation.ValidateTrackTitle(input.Title);
        if (!validation.IsValid)
            return OperationResult<TrackDto>.Invalid(validation.Errors);

        track.Title = input.Title.Trim();
        track.IsFocusTrack = input.IsFocusTrack;
        await db.SaveChangesAsync();

        return OperationResult<TrackDto>.Success(ToDto(track));
    }

    public async Task<OperationResult<TrackDto>> ToggleFocusAsync(Guid id)
    {
        var track = await db.Tracks.FindAsync(id);
        if (track is null) return OperationResult<TrackDto>.NotFound();

        track.IsFocusTrack = !track.IsFocusTrack;
        await db.SaveChangesAsync();

        return OperationResult<TrackDto>.Success(ToDto(track));
    }

    public async Task<OperationResult> ReorderAsync(Guid releaseId, ReorderTracksInput input)
    {
        var tracks = await db.Tracks
            .Where(t => t.ReleaseId == releaseId)
            .ToListAsync();
        if (tracks.Count == 0) return OperationResult.NotFound();

        // Every track on the release must appear exactly once; TrackNumber stays 1-based.
        var applied = Reorder.TryApply(tracks, input.OrderedTrackIds, t => t.Id, (t, i) => t.TrackNumber = i + 1);
        if (!applied)
            return OperationResult.Invalid(new[] { "Reorder must list every track on the release exactly once." });

        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        var track = await db.Tracks.FindAsync(id);
        if (track is null) return OperationResult.NotFound();

        db.Tracks.Remove(track);
        // Renumber the survivors so TrackNumber stays contiguous.
        var rest = await db.Tracks
            .Where(t => t.ReleaseId == track.ReleaseId && t.Id != id)
            .OrderBy(t => t.TrackNumber)
            .ToListAsync();
        for (var i = 0; i < rest.Count; i++)
            rest[i].TrackNumber = i + 1;

        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    private static TrackDto ToDto(Track t) =>
        new(t.Id, t.TrackNumber, t.Title, t.IsFocusTrack);
}
