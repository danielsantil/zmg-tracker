using Zmg.Domain;

namespace Zmg.Api.Services.Interfaces;

public interface IPendingService
{
    Task<IReadOnlyList<PendingAction>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PendingAction>> ListByReleaseIdAsync(Guid releaseId, CancellationToken ct = default);
}
