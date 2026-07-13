using Zmg.Domain;

namespace Zmg.Api.Services.Interfaces;

public interface IPendingService
{
    Task<IReadOnlyList<PendingAction>> ListAsync();
}
