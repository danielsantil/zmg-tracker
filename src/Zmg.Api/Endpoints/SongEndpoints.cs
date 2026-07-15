using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;

namespace Zmg.Api.Endpoints;

/// <summary>
/// Catalog endpoints (M13). Songs are the creative works released through one or more releases;
/// their UPCs/release date are derived from those links. Logic lives in ISongService.
/// </summary>
public static class SongEndpoints
{
    public static void MapSongEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/songs").WithTags("Songs");

        // List; q filters by title, scope=archived returns archived songs (default: active).
        group.MapGet("", async (string? q, string? scope, ISongService songs) =>
            Results.Ok(await songs.ListAsync(q, scope)));

        // Detail with feats/collabs and every linked release.
        group.MapGet("/{id:guid}", async (Guid id, ISongService songs) =>
            (await songs.GetAsync(id)).ToOk());

        // Edit title / main artist / ISRC / feats-collabs. Returns the updated song + any warnings.
        group.MapPut("/{id:guid}", async (Guid id, SongUpdateInput input, ISongService songs) =>
            (await songs.UpdateAsync(id, input)).ToOkWithWarnings());

        // Archive: terminal, non-restorable (M15). Mostly orphans — active-release songs cascade via the release.
        group.MapPost("/{id:guid}/archive", async (Guid id, ISongService songs) =>
            (await songs.ArchiveAsync(id)).ToNoContent());

        // Remove: soft-delete, allowed for an archived song or an orphan. Songs are never hard-deleted. M15.
        group.MapDelete("/{id:guid}", async (Guid id, ISongService songs) =>
            (await songs.DeleteAsync(id)).ToNoContent());
    }
}
