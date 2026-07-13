using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain.Enums;

namespace Zmg.Api.Endpoints;

public static class ReleaseEndpoints
{
    public static void MapReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/releases");

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

        group.MapDelete("/{id:guid}", async (Guid id, IReleaseService releases) =>
            (await releases.DeleteAsync(id)).ToNoContent());
    }
}
