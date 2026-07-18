using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Domain.Enums;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Template management (M3). Edits here only shape *future* releases — existing releases
/// own a snapshot copy taken on create, so nothing here touches live checklists.
/// </summary>
public sealed class TemplateService(ZmgDbContext db) : ITemplateService
{
    public async Task<IReadOnlyList<TemplateDto>> ListAsync(CancellationToken ct = default)
    {
        var templates = await db.ChecklistTemplates.AsNoTracking()
            .Include(t => t.Tasks)
            .OrderBy(t => t.Type)
            .ToListAsync(ct);

        return templates.Select(ToDto).ToList();
    }

    public async Task<OperationResult<TemplateTaskDto>> AddTaskAsync(Guid templateId, AddTemplateTaskInput input, CancellationToken ct = default)
    {
        if (!await db.ChecklistTemplates.AnyAsync(t => t.Id == templateId, ct))
            return OperationResult<TemplateTaskDto>.NotFound();

        var validation = Validation.ValidateTaskTitle(input.Title);
        if (!validation.IsValid)
            return OperationResult<TemplateTaskDto>.Invalid(validation.Errors);

        var nextOrder = await NextSortOrder(templateId, input.Phase, ct: ct);

        var task = new TemplateTask
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = templateId,
            Title = input.Title.Trim(),
            Phase = input.Phase,
            SortOrder = nextOrder,
            MinDaysBefore = input.MinDaysBefore,
            MaxDaysBefore = input.MaxDaysBefore,
        };
        db.TemplateTasks.Add(task);
        await db.SaveChangesAsync(ct);

        return OperationResult<TemplateTaskDto>.Success(ToDto(task));
    }

    public async Task<OperationResult<TemplateTaskDto>> UpdateTaskAsync(Guid id, UpdateTemplateTaskInput input, CancellationToken ct = default)
    {
        var task = await db.TemplateTasks.FindAsync([id], ct);
        if (task is null) return OperationResult<TemplateTaskDto>.NotFound();

        var validation = Validation.ValidateTaskTitle(input.Title);
        if (!validation.IsValid)
            return OperationResult<TemplateTaskDto>.Invalid(validation.Errors);

        // Moving to a new phase appends to the end of the target phase.
        if (task.Phase != input.Phase)
        {
            task.SortOrder = await NextSortOrder(task.ChecklistTemplateId, input.Phase, excludeTaskId: id, ct: ct);
            task.Phase = input.Phase;
        }

        task.Title = input.Title.Trim();
        task.MinDaysBefore = input.MinDaysBefore;
        task.MaxDaysBefore = input.MaxDaysBefore;
        await db.SaveChangesAsync(ct);

        return OperationResult<TemplateTaskDto>.Success(ToDto(task));
    }

    public async Task<OperationResult> ReorderTasksAsync(Guid templateId, ReorderTemplateTasksInput input, CancellationToken ct = default)
    {
        var tasks = await db.TemplateTasks
            .Where(t => t.ChecklistTemplateId == templateId && t.Phase == input.Phase)
            .ToListAsync(ct);
        if (tasks.Count == 0) return OperationResult.NotFound();

        var applied = Reorder.TryApply(tasks, input.OrderedTaskIds, t => t.Id, (t, i) => t.SortOrder = i);
        if (!applied)
            return OperationResult.Invalid(new[] { "Reorder must list every task in the phase exactly once." });

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteTaskAsync(Guid id, CancellationToken ct = default)
    {
        var task = await db.TemplateTasks.FindAsync([id], ct);
        if (task is null) return OperationResult.NotFound();

        var remaining = await db.TemplateTasks
            .CountAsync(t => t.ChecklistTemplateId == task.ChecklistTemplateId && t.Id != id, ct);
        var validation = Validation.ValidateTemplateTaskDelete(remaining);
        if (!validation.IsValid)
            return OperationResult.Conflict(validation.Errors);

        db.TemplateTasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    // Append position for a task added to (or moved into) a phase: one past the current max.
    private async Task<int> NextSortOrder(Guid templateId, Phase phase, Guid? excludeTaskId = null, CancellationToken ct = default) =>
        (await db.TemplateTasks
            .Where(t => t.ChecklistTemplateId == templateId && t.Phase == phase
                && (excludeTaskId == null || t.Id != excludeTaskId))
            .Select(t => (int?)t.SortOrder)
            .MaxAsync(ct) ?? -1) + 1;

    private static TemplateDto ToDto(ChecklistTemplate template)
    {
        var phases = Enum.GetValues<Phase>()
            .Select(phase => new TemplatePhaseGroupDto(
                phase,
                template.Tasks
                    .Where(t => t.Phase == phase)
                    .OrderBy(t => t.SortOrder)
                    .Select(ToDto)
                    .ToList()))
            .ToList();
        return new TemplateDto(template.Id, template.Type, phases);
    }

    private static TemplateTaskDto ToDto(TemplateTask t) =>
        new(t.Id, t.Title, t.Phase, t.SortOrder, t.MinDaysBefore, t.MaxDaysBefore);
}
