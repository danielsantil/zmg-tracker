using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface ISongService
{
    Task<IReadOnlyList<SongListItemDto>> ListAsync(string? q, string? scope, Guid? artistId);
    Task<OperationResult<SongDetailDto>> GetAsync(Guid id);
    Task<OperationResult<SongDetailDto>> CreateAsync(SongCreateInput input);
    Task<OperationResult<SongDetailDto>> UpdateAsync(Guid id, SongUpdateInput input);
    Task<OperationResult> ArchiveAsync(Guid id);
    Task<OperationResult> DeleteAsync(Guid id);
}
