using Microsoft.EntityFrameworkCore;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Aggregates pending actions across every release in the global order (task-due nearest-first,
/// then data/missing-identifier items). The pure engine lives in <see cref="PendingActions"/>;
/// this just loads the graph it needs and applies the ordering.
/// </summary>
public sealed class PendingService(ZmgDbContext db) : IPendingService
{
    public async Task<IReadOnlyList<PendingAction>> ListAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var releases = await db.Releases
            .Include(r => r.MainArtist)
            .Include(r => r.Tasks)
            .ToListAsync();

        return PendingActions.Order(
            releases.SelectMany(r => PendingActions.Compute(r, today)));
    }
}
