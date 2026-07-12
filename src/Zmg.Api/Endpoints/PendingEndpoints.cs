using Microsoft.EntityFrameworkCore;
using Zmg.Api.Data;
using Zmg.Domain;

namespace Zmg.Api.Endpoints;

public static class PendingEndpoints
{
    public static void MapPendingEndpoints(this IEndpointRouteBuilder app)
    {
        // Aggregate pending actions across every release, in the global order (task-due nearest-first,
        // then data/missing-identifier items). The Home "Pending Tasks" section renders this whole list.
        app.MapGet("/api/pending", async (ZmgDbContext db) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var releases = await db.Releases
                .Include(r => r.MainArtist)
                .Include(r => r.Tasks)
                .ToListAsync();

            var actions = PendingActions.Order(
                releases.SelectMany(r => PendingActions.Compute(r, r.Tasks, today)));

            return Results.Ok(actions);
        });
    }
}
