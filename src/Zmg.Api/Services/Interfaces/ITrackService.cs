using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface ITrackService
{
    Task<OperationResult<TrackDto>> AddAsync(Guid releaseId, TrackInput input);
    Task<OperationResult<TrackDto>> ToggleFocusAsync(Guid releaseId, Guid songId);
    Task<OperationResult> ReorderAsync(Guid releaseId, ReorderTracksInput input);
    Task<OperationResult> DeleteAsync(Guid releaseId, Guid songId);
}
