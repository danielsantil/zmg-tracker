using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface IArtistService
{
    Task<IReadOnlyList<ArtistDto>> ListAsync(CancellationToken ct = default);
    Task<OperationResult<ArtistDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<ArtistDto>> CreateAsync(ArtistInput input, CancellationToken ct = default);
    Task<OperationResult<ArtistDto>> UpdateAsync(Guid id, ArtistInput input, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
