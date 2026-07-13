using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface ITrackService
{
    Task<OperationResult<TrackDto>> AddAsync(Guid releaseId, AddTrackInput input);
    Task<OperationResult<TrackDto>> UpdateAsync(Guid id, UpdateTrackInput input);
    Task<OperationResult<TrackDto>> ToggleFocusAsync(Guid id);
    Task<OperationResult> ReorderAsync(Guid releaseId, ReorderTracksInput input);
    Task<OperationResult> DeleteAsync(Guid id);
}
