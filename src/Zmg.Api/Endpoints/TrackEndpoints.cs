using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;

namespace Zmg.Api.Endpoints;

/// <summary>
/// Album track list (M4). Tracks belong to a release (albums in practice; the UI only
/// surfaces the list for album releases). Ordering is by TrackNumber, which stays 1-based
/// and contiguous; reorder rewrites it from the given id order. Logic lives in ITrackService.
/// </summary>
public static class TrackEndpoints
{
    public static void MapTrackEndpoints(this IEndpointRouteBuilder app)
    {
        // Add a track to a release, appended after the current last track.
        app.MapPost("/api/releases/{releaseId:guid}/tracks", async (Guid releaseId, AddTrackInput input, ITrackService tracks) =>
            (await tracks.AddAsync(releaseId, input)).ToCreated(t => $"/api/tracks/{t.Id}"));

        // Rename and/or set the focus-track flag.
        app.MapPut("/api/tracks/{id:guid}", async (Guid id, UpdateTrackInput input, ITrackService tracks) =>
            (await tracks.UpdateAsync(id, input)).ToOk());

        // Toggle the focus-track flag (the quick daily action).
        app.MapPatch("/api/tracks/{id:guid}/focus", async (Guid id, ITrackService tracks) =>
            (await tracks.ToggleFocusAsync(id)).ToOk());

        // Reorder tracks; TrackNumber follows the given id order (1-based).
        app.MapPut("/api/releases/{releaseId:guid}/tracks/order", async (Guid releaseId, ReorderTracksInput input, ITrackService tracks) =>
            (await tracks.ReorderAsync(releaseId, input)).ToNoContent());

        // Delete a track and close the gap in numbering.
        app.MapDelete("/api/tracks/{id:guid}", async (Guid id, ITrackService tracks) =>
            (await tracks.DeleteAsync(id)).ToNoContent());
    }
}
