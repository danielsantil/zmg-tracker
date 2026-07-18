using Zmg.Api.Contracts;
using Zmg.Domain.Enums;

namespace Zmg.Api.Services.Interfaces;

public interface IReleaseService
{
    Task<IReadOnlyList<ReleaseListItemDto>> ListAsync(
        Guid? artistId, ReleaseType? type, string? status, string? scope, string? q, CancellationToken ct = default);
    Task<OperationResult<ReleaseDetailDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<ReleaseDetailDto>> CreateAsync(ReleaseInput input, CancellationToken ct = default);
    Task<OperationResult<ReleaseDetailDto>> UpdateAsync(Guid id, ReleaseInput input, CancellationToken ct = default);
    Task<OperationResult<ArchivePreviewDto>> GetArchivePreviewAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> ArchiveAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
