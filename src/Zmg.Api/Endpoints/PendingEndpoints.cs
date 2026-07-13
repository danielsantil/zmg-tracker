using Zmg.Api.Services.Interfaces;

namespace Zmg.Api.Endpoints;

public static class PendingEndpoints
{
    public static void MapPendingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pending").WithTags("PendingActions");
        
        // Aggregate pending actions across every release, in the global order (task-due nearest-first,
        // then data/missing-identifier items). The Home "Pending Tasks" section renders this whole list.
        group.MapGet("", async (IPendingService pending) =>
            Results.Ok(await pending.ListAsync()));

        // Gets pending actions for a release
        group.MapGet("/{id:guid}", async (Guid id, IPendingService pending) => 
            Results.Ok(await pending.ListByReleaseIdAsync(id)));
    }
}
