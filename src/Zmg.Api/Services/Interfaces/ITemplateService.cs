using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

public interface ITemplateService
{
    Task<IReadOnlyList<TemplateDto>> ListAsync();
    Task<OperationResult<TemplateTaskDto>> AddTaskAsync(Guid templateId, AddTemplateTaskInput input);
    Task<OperationResult<TemplateTaskDto>> UpdateTaskAsync(Guid id, UpdateTemplateTaskInput input);
    Task<OperationResult> ReorderTasksAsync(Guid templateId, ReorderTemplateTasksInput input);
    Task<OperationResult> DeleteTaskAsync(Guid id);
}
