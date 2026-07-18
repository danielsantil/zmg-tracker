using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface IReleaseTaskService
{
    Task<OperationResult<ReleaseTaskDto>> AddAsync(Guid releaseId, AddTaskInput input, CancellationToken ct = default);
    Task<OperationResult<ReleaseTaskDto>> UpdateAsync(Guid id, UpdateTaskInput input, CancellationToken ct = default);
    Task<OperationResult<ReleaseTaskDto>> ToggleAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> ReorderAsync(Guid releaseId, ReorderTasksInput input, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
