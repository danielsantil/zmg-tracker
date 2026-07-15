using Microsoft.EntityFrameworkCore;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Aggregates pending actions across every release and song in the global order (task-due nearest-first,
/// then the data kinds by subject). The pure engine lives in <see cref="PendingActions"/>; this just loads
/// the graph it needs, precomputes the per-song distributed flag, and applies the ordering.
/// </summary>
public sealed class PendingService(ZmgDbContext db) : IPendingService
{
    public async Task<IReadOnlyList<PendingAction>> ListAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Release-owned actions (task-due, missing-UPC, empty-album). Tracks drive the empty-album nag.
        var releases = await db.Releases
            .Where(r => r.ArchivedAt == null) // archived releases are read-only; no actions
            .Include(r => r.MainArtist)
            .Include(r => r.Tasks)
            .Include(r => r.Tracks)
            .ToListAsync();
        var releaseActions = releases.SelectMany(r => PendingActions.Compute(r, today));

        // Song-owned actions (missing-ISRC). A song is "distributed" when any linked, non-archived
        // release has its Distribute-to-DSPs task checked; deleted links are already hidden by the filter.
        var songs = await db.Songs
            .Where(s => s.ArchivedAt == null)
            .Include(s => s.MainArtist)
            .Include(s => s.ReleaseLinks).ThenInclude(t => t.Release).ThenInclude(r => r!.Tasks)
            .ToListAsync();
        var songActions = songs.SelectMany(s => PendingActions.ComputeForSong(s, IsDistributed(s)));

        return PendingActions.Order(releaseActions.Concat(songActions));
    }

    public async Task<IReadOnlyList<PendingAction>> ListByReleaseIdAsync(Guid releaseId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var release = await db.Releases
            .Include(r => r.MainArtist)
            .Include(r => r.Tasks)
            .Include(r => r.Tracks).ThenInclude(t => t.Song).ThenInclude(s => s!.MainArtist)
            .Include(r => r.Tracks).ThenInclude(t => t.Song).ThenInclude(s => s!.ReleaseLinks)
                .ThenInclude(t => t.Release).ThenInclude(r => r!.Tasks)
            .FirstOrDefaultAsync(r => r.Id == releaseId);

        // Archived releases are read-only — surface no pending actions on their detail either.
        if (release is null || release.IsArchived) return [];

        // The release's own actions plus a rolled-up "tracks missing ISRC" view for its songs.
        var actions = PendingActions.Compute(release, today).ToList();
        foreach (var song in release.Tracks.Select(t => t.Song).Where(s => s is not null))
            actions.AddRange(PendingActions.ComputeForSong(song!, IsDistributed(song!)));

        return PendingActions.Order(actions);
    }

    /// <summary>A song is distributed when any linked, non-archived release has been distributed to DSPs.</summary>
    private static bool IsDistributed(Song song) => song.ReleaseLinks
        .Where(t => t.Release is { ArchivedAt: null })
        .Any(t => t.Release!.IsDistributed);
}
