using Zmg.Api.Contracts;
using Zmg.Domain.Enums;

namespace Zmg.Api.Services.Interfaces;

public interface IReleaseService
{
    Task<IReadOnlyList<ReleaseListItemDto>> ListAsync(
        Guid? artistId, ReleaseType? type, string? status, string? scope, string? q);
    Task<OperationResult<ReleaseDetailDto>> GetAsync(Guid id);
    Task<OperationResult<ReleaseDetailDto>> CreateAsync(ReleaseInput input);
    Task<OperationResult<ReleaseDetailDto>> UpdateAsync(Guid id, ReleaseInput input);
    Task<OperationResult> ArchiveAsync(Guid id);
    Task<OperationResult> DeleteAsync(Guid id);
}
