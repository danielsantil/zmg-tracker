using Zmg.Api.Services.Interfaces;

namespace Zmg.Api.Endpoints;

public static class PendingEndpoints
{
    public static void MapPendingEndpoints(this IEndpointRouteBuilder app)
    {
        // Aggregate pending actions across every release, in the global order (task-due nearest-first,
        // then data/missing-identifier items). The Home "Pending Tasks" section renders this whole list.
        app.MapGet("/api/pending", async (IPendingService pending) =>
            Results.Ok(await pending.ListAsync()));
    }
}
