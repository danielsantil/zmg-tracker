using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;

namespace Zmg.Api.Endpoints;

/// <summary>
/// Album track list (v2.0). Tracks are a Release↔Song join addressed by songId, all under the
/// release group. Ordering is by TrackNumber (1-based, contiguous); reorder rewrites it from the
/// given songId order. Singles are fixed at one track. Logic lives in ITrackService.
/// </summary>
public static class TrackEndpoints
{
    public static void MapTrackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/releases").WithTags("Tracks");

        // Add a track (existing catalog song or new inline song), appended after the last track.
        group.MapPost("/{releaseId:guid}/tracks", async (Guid releaseId, TrackInput input, ITrackService tracks) =>
            (await tracks.AddAsync(releaseId, input)).ToCreated(t => $"/api/releases/{releaseId}/tracks/{t.SongId}"));

        // Toggle the focus-track flag (the quick daily action).
        group.MapPatch("/{releaseId:guid}/tracks/{songId:guid}/focus", async (Guid releaseId, Guid songId, ITrackService tracks) =>
            (await tracks.ToggleFocusAsync(releaseId, songId)).ToOk());

        // Reorder tracks; TrackNumber follows the given songId order (1-based).
        group.MapPut("/{releaseId:guid}/tracks/order", async (Guid releaseId, ReorderTracksInput input, ITrackService tracks) =>
            (await tracks.ReorderAsync(releaseId, input)).ToNoContent());

        // Remove a track (the join only; the song survives) and close the gap in numbering.
        group.MapDelete("/{releaseId:guid}/tracks/{songId:guid}", async (Guid releaseId, Guid songId, ITrackService tracks) =>
            (await tracks.DeleteAsync(releaseId, songId)).ToNoContent());
    }
}
