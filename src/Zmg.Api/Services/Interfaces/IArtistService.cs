using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface IArtistService
{
    Task<IReadOnlyList<ArtistDto>> ListAsync();
    Task<OperationResult<ArtistDto>> CreateAsync(ArtistInput input);
    Task<OperationResult<ArtistDto>> UpdateAsync(Guid id, ArtistInput input);
    Task<OperationResult> DeleteAsync(Guid id);
}
