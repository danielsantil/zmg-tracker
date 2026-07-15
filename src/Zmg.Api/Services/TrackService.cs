using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain.Entities;
using Zmg.Domain.Enums;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Album track list (v2.0: a Release↔Song join, addressed by songId). Adding a track either links an
/// existing catalog song or creates a new inline song (main artist inherited from the release).
/// TrackNumber stays 1-based and contiguous; add appends, delete closes the gap, reorder rewrites it.
/// Singles are fixed at one track (their track is set at create), so add/delete are blocked on them.
/// </summary>
public sealed class TrackService(ZmgDbContext db) : ITrackService
{
    public async Task<OperationResult<TrackDto>> AddAsync(Guid releaseId, TrackInput input)
    {
        var release = await db.Releases.FirstOrDefaultAsync(r => r.Id == releaseId);
        if (release is null) return OperationResult<TrackDto>.NotFound();

        if (release.Type == ReleaseType.Single)
            return OperationResult<TrackDto>.Conflict(new[] { "Singles carry exactly one track." });

        var hasId = input.SongId is { } sid && sid != Guid.Empty;
        var hasTitle = !string.IsNullOrWhiteSpace(input.Title);
        if (hasId == hasTitle)
            return OperationResult<TrackDto>.Invalid(new[] { "Each track must be either an existing song or a new title, not both or neither." });

        Guid songId;
        if (hasId)
        {
            var existingId = input.SongId!.Value;
            var song = await db.Songs.FirstOrDefaultAsync(s => s.Id == existingId);
            if (song is null) return OperationResult<TrackDto>.Invalid(new[] { "Selected song does not exist." });
            if (song.IsArchived) return OperationResult<TrackDto>.Conflict(new[] { "Can't add an archived song to a release." });

            if (await db.Tracks.AnyAsync(t => t.ReleaseId == releaseId && t.SongId == existingId))
                return OperationResult<TrackDto>.Conflict(new[] { "That song is already on this release." });

            songId = existingId;
        }
        else
        {
            var song = SongMapping.NewSong(release.MainArtistId, input.Title!, input.Isrc, input.Artists);
            db.Songs.Add(song);
            songId = song.Id;
        }

        var lastNumber = await db.Tracks
            .Where(t => t.ReleaseId == releaseId)
            .Select(t => (int?)t.TrackNumber)
            .MaxAsync() ?? 0;

        db.Tracks.Add(new Track
        {
            ReleaseId = releaseId,
            SongId = songId,
            TrackNumber = lastNumber + 1,
            IsFocusTrack = false,
        });
        await db.SaveChangesAsync();

        return OperationResult<TrackDto>.Success(await LoadDto(releaseId, songId));
    }

    public async Task<OperationResult<TrackDto>> ToggleFocusAsync(Guid releaseId, Guid songId)
    {
        var track = await db.Tracks.FirstOrDefaultAsync(t => t.ReleaseId == releaseId && t.SongId == songId);
        if (track is null) return OperationResult<TrackDto>.NotFound();

        track.IsFocusTrack = !track.IsFocusTrack;
        await db.SaveChangesAsync();

        return OperationResult<TrackDto>.Success(await LoadDto(releaseId, songId));
    }

    public async Task<OperationResult> ReorderAsync(Guid releaseId, ReorderTracksInput input)
    {
        var tracks = await db.Tracks
            .Where(t => t.ReleaseId == releaseId)
            .ToListAsync();
        if (tracks.Count == 0) return OperationResult.NotFound();

        // Every track on the release must appear exactly once (keyed by songId); TrackNumber stays 1-based.
        var applied = Reorder.TryApply(tracks, input.OrderedSongIds, t => t.SongId, (t, i) => t.TrackNumber = i + 1);
        if (!applied)
            return OperationResult.Invalid(new[] { "Reorder must list every track on the release exactly once." });

        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(Guid releaseId, Guid songId)
    {
        var track = await db.Tracks.FirstOrDefaultAsync(t => t.ReleaseId == releaseId && t.SongId == songId);
        if (track is null) return OperationResult.NotFound();

        var release = await db.Releases.FindAsync(releaseId);
        if (release is { Type: ReleaseType.Single })
            return OperationResult.Conflict(new[] { "Singles carry exactly one track." });

        // Remove only the join — the Song survives (possibly as an orphan). Renumber the survivors.
        db.Tracks.Remove(track);
        var rest = await db.Tracks
            .Where(t => t.ReleaseId == releaseId && t.SongId != songId)
            .OrderBy(t => t.TrackNumber)
            .ToListAsync();
        for (var i = 0; i < rest.Count; i++)
            rest[i].TrackNumber = i + 1;

        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    private async Task<TrackDto> LoadDto(Guid releaseId, Guid songId)
    {
        var track = await db.Tracks
            .Include(t => t.Song!).ThenInclude(s => s.Artists).ThenInclude(sa => sa.Artist)
            .FirstAsync(t => t.ReleaseId == releaseId && t.SongId == songId);
        return SongMapping.ToDto(track);
    }
}
