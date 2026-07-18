using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Domain.Enums;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Album track list (v2.0: a Release↔Song join, addressed by songId). Adding a track either links an
/// existing catalog song or creates a new inline song (main artist inherited from the release).
/// TrackNumber stays 1-based and contiguous; add appends, delete closes the gap, reorder rewrites it.
/// Singles are fixed at one track (their track is set at create), so add/delete are blocked on them.
/// Every write is gated on <see cref="ReleaseMutability"/> — archived releases are read-only (M25).
/// </summary>
public sealed class TrackService(ZmgDbContext db) : ITrackService
{
    public async Task<OperationResult<TrackDto>> AddAsync(Guid releaseId, TrackInput input, CancellationToken ct = default)
    {
        var release = await db.Releases.FirstOrDefaultAsync(r => r.Id == releaseId, ct);
        if (release is null) return OperationResult<TrackDto>.NotFound();
        if (!ReleaseMutability.CanEdit(release.IsArchived))
            return OperationResult<TrackDto>.Conflict(new[] { ReleaseMutability.ArchivedReadOnlyMessage });

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
            var song = await db.Songs.FirstOrDefaultAsync(s => s.Id == existingId, ct);
            if (song is null) return OperationResult<TrackDto>.Invalid(new[] { "Selected song does not exist." });
            if (song.IsArchived) return OperationResult<TrackDto>.Conflict(new[] { "Can't add an archived song to a release." });

            if (await db.Tracks.AnyAsync(t => t.ReleaseId == releaseId && t.SongId == existingId, ct))
                return OperationResult<TrackDto>.Conflict(new[] { "That song is already on this release." });

            songId = existingId;
        }
        else
        {
            // Song titles are unique per main artist: reject a new inline title that clashes with an
            // active same-artist song. The rule is pure Domain (Validation.ValidateSong) — the SPA turns
            // this into a "pick the existing / rename" prompt.
            var activeTitles = await db.Songs.AsNoTracking()
                .Where(s => s.MainArtistId == release.MainArtistId && s.ArchivedAt == null)
                .Select(s => s.Title)
                .ToListAsync(ct);
            var titleCheck = Validation.ValidateSong(input.Title, release.MainArtistId, mainArtistExists: true, activeTitles);
            if (!titleCheck.IsValid)
                return OperationResult<TrackDto>.Invalid(titleCheck.Errors);

            var song = SongMapping.NewSong(release.MainArtistId, input.Title!, input.Isrc, input.Artists);
            db.Songs.Add(song);
            songId = song.Id;
        }

        var lastNumber = await db.Tracks
            .Where(t => t.ReleaseId == releaseId)
            .Select(t => (int?)t.TrackNumber)
            .MaxAsync(ct) ?? 0;

        db.Tracks.Add(new Track
        {
            ReleaseId = releaseId,
            SongId = songId,
            TrackNumber = lastNumber + 1,
            IsFocusTrack = false,
        });
        await db.SaveChangesAsync(ct);

        return OperationResult<TrackDto>.Success(await LoadDto(releaseId, songId, ct));
    }

    public async Task<OperationResult<TrackDto>> ToggleFocusAsync(Guid releaseId, Guid songId, CancellationToken ct = default)
    {
        var track = await db.Tracks.FirstOrDefaultAsync(t => t.ReleaseId == releaseId && t.SongId == songId, ct);
        if (track is null) return OperationResult<TrackDto>.NotFound();
        if (await IsArchived(releaseId, ct))
            return OperationResult<TrackDto>.Conflict(new[] { ReleaseMutability.ArchivedReadOnlyMessage });

        track.IsFocusTrack = !track.IsFocusTrack;
        await db.SaveChangesAsync(ct);

        return OperationResult<TrackDto>.Success(await LoadDto(releaseId, songId, ct));
    }

    public async Task<OperationResult> ReorderAsync(Guid releaseId, ReorderTracksInput input, CancellationToken ct = default)
    {
        var tracks = await db.Tracks
            .Where(t => t.ReleaseId == releaseId)
            .ToListAsync(ct);
        if (tracks.Count == 0) return OperationResult.NotFound();
        if (await IsArchived(releaseId, ct))
            return OperationResult.Conflict(new[] { ReleaseMutability.ArchivedReadOnlyMessage });

        // Every track on the release must appear exactly once (keyed by songId); TrackNumber stays 1-based.
        var applied = Reorder.TryApply(tracks, input.OrderedSongIds, t => t.SongId, (t, i) => t.TrackNumber = i + 1);
        if (!applied)
            return OperationResult.Invalid(new[] { "Reorder must list every track on the release exactly once." });

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(Guid releaseId, Guid songId, CancellationToken ct = default)
    {
        var track = await db.Tracks.FirstOrDefaultAsync(t => t.ReleaseId == releaseId && t.SongId == songId, ct);
        if (track is null) return OperationResult.NotFound();

        var release = await db.Releases.FindAsync([releaseId], ct);
        if (release is not null && !ReleaseMutability.CanEdit(release.IsArchived))
            return OperationResult.Conflict(new[] { ReleaseMutability.ArchivedReadOnlyMessage });
        if (release is { Type: ReleaseType.Single })
            return OperationResult.Conflict(new[] { "Singles carry exactly one track." });

        // Remove only the join — the Song survives (possibly as an orphan). Renumber the survivors.
        db.Tracks.Remove(track);
        var rest = await db.Tracks
            .Where(t => t.ReleaseId == releaseId && t.SongId != songId)
            .OrderBy(t => t.TrackNumber)
            .ToListAsync(ct);
        for (var i = 0; i < rest.Count; i++)
            rest[i].TrackNumber = i + 1;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    private Task<bool> IsArchived(Guid releaseId, CancellationToken ct) =>
        db.Releases.AsNoTracking().AnyAsync(r => r.Id == releaseId && r.ArchivedAt != null, ct);

    private async Task<TrackDto> LoadDto(Guid releaseId, Guid songId, CancellationToken ct)
    {
        var track = await db.Tracks
            .Include(t => t.Song!).ThenInclude(s => s.Artists).ThenInclude(sa => sa.Artist)
            .FirstAsync(t => t.ReleaseId == releaseId && t.SongId == songId, ct);
        return SongMapping.ToDto(track);
    }
}
