using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface IReleaseTaskService
{
    Task<OperationResult<ReleaseTaskDto>> AddAsync(Guid releaseId, AddTaskInput input);
    Task<OperationResult<ReleaseTaskDto>> UpdateAsync(Guid id, UpdateTaskInput input);
    Task<OperationResult<ReleaseTaskDto>> ToggleAsync(Guid id);
    Task<OperationResult> ReorderAsync(Guid releaseId, ReorderTasksInput input);
    Task<OperationResult> DeleteAsync(Guid id);
}
