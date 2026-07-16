using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain.Enums;

namespace Zmg.Api.Endpoints;

public static class ReleaseEndpoints
{
    public static void MapReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/releases").WithTags("Releases");

        // List with progress counts (done/total) and derived status. Filterable.
        group.MapGet("", async (Guid? artistId, ReleaseType? type, string? status, string? scope, string? q, IReleaseService releases) =>
            Results.Ok(await releases.ListAsync(artistId, type, status, scope, q)));

        // Detail with tasks grouped by phase.
        group.MapGet("/{id:guid}", async (Guid id, IReleaseService releases) =>
            (await releases.GetAsync(id)).ToOk());

        // Create; copies the default template for the type onto the release.
        group.MapPost("", async (ReleaseInput input, IReleaseService releases) =>
            (await releases.CreateAsync(input)).ToCreatedWithWarnings(r => $"/api/releases/{r.Id}"));

        group.MapPut("/{id:guid}", async (Guid id, ReleaseInput input, IReleaseService releases) =>
            (await releases.UpdateAsync(id, input)).ToOkWithWarnings());

        // Preview which songs would cascade-archive with this release, so the UI can warn first (2.0).
        group.MapGet("/{id:guid}/archive-preview", async (Guid id, IReleaseService releases) =>
            (await releases.GetArchivePreviewAsync(id)).ToOk());

        // Archive a release (terminal, non-restorable; only for releaseDate >= today). v1.2.
        group.MapPost("/{id:guid}/archive", async (Guid id, IReleaseService releases) =>
            (await releases.ArchiveAsync(id)).ToNoContent());

        // Remove: soft-delete, reachable only from an archived release. Releases are never hard-deleted. v1.2.
        group.MapDelete("/{id:guid}", async (Guid id, IReleaseService releases) =>
            (await releases.DeleteAsync(id)).ToNoContent());
    }
}
