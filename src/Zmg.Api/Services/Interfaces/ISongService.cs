using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface ISongService
{
    Task<IReadOnlyList<SongListItemDto>> ListAsync(string? q, string? scope, Guid? artistId, CancellationToken ct = default);
    Task<OperationResult<SongDetailDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<SongDetailDto>> CreateAsync(SongCreateInput input, CancellationToken ct = default);
    Task<OperationResult<SongDetailDto>> UpdateAsync(Guid id, SongUpdateInput input, CancellationToken ct = default);
    Task<OperationResult> ArchiveAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
