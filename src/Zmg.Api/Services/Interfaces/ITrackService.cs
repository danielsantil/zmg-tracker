using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface ITrackService
{
    Task<OperationResult<TrackDto>> AddAsync(Guid releaseId, TrackInput input, CancellationToken ct = default);
    Task<OperationResult<TrackDto>> ToggleFocusAsync(Guid releaseId, Guid songId, CancellationToken ct = default);
    Task<OperationResult> ReorderAsync(Guid releaseId, ReorderTracksInput input, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid releaseId, Guid songId, CancellationToken ct = default);
}
