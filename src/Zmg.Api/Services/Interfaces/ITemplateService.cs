using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface ITemplateService
{
    Task<IReadOnlyList<TemplateDto>> ListAsync(CancellationToken ct = default);
    Task<OperationResult<TemplateTaskDto>> AddTaskAsync(Guid templateId, AddTemplateTaskInput input, CancellationToken ct = default);
    Task<OperationResult<TemplateTaskDto>> UpdateTaskAsync(Guid id, UpdateTemplateTaskInput input, CancellationToken ct = default);
    Task<OperationResult> ReorderTasksAsync(Guid templateId, ReorderTemplateTasksInput input, CancellationToken ct = default);
    Task<OperationResult> DeleteTaskAsync(Guid id, CancellationToken ct = default);
}
