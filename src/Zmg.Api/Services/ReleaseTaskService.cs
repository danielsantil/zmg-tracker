using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Domain.Enums;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Live-checklist task mutations (M2). Toggling is the daily action, so these stay
/// small and single-purpose; the frontend recomputes progress from the task list.
/// </summary>
public sealed class ReleaseTaskService(ZmgDbContext db) : IReleaseTaskService
{
    public async Task<OperationResult<ReleaseTaskDto>> AddAsync(Guid releaseId, AddTaskInput input)
    {
        if (!await db.Releases.AnyAsync(r => r.Id == releaseId))
            return OperationResult<ReleaseTaskDto>.NotFound();

        var validation = Validation.ValidateTaskTitle(input.Title);
        if (!validation.IsValid)
            return OperationResult<ReleaseTaskDto>.Invalid(validation.Errors);

        var task = new ReleaseTask
        {
            Id = Guid.NewGuid(),
            ReleaseId = releaseId,
            Title = input.Title.Trim(),
            Phase = input.Phase,
            SortOrder = await NextSortOrder(releaseId, input.Phase),
            IsDone = false,
            MinDaysBefore = input.MinDaysBefore,
            MaxDaysBefore = input.MaxDaysBefore,
        };
        db.ReleaseTasks.Add(task);
        await db.SaveChangesAsync();

        return OperationResult<ReleaseTaskDto>.Success(ToDto(task));
    }

    public async Task<OperationResult<ReleaseTaskDto>> UpdateAsync(Guid id, UpdateTaskInput input)
    {
        var task = await db.ReleaseTasks.FindAsync(id);
        if (task is null) return OperationResult<ReleaseTaskDto>.NotFound();

        var validation = Validation.ValidateTaskTitle(input.Title);
        if (!validation.IsValid)
            return OperationResult<ReleaseTaskDto>.Invalid(validation.Errors);

        // Moving to a new phase appends to the end of the target phase.
        if (task.Phase != input.Phase)
        {
            task.SortOrder = await NextSortOrder(task.ReleaseId, input.Phase, excludeTaskId: id);
            task.Phase = input.Phase;
        }

        task.Title = input.Title.Trim();
        task.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        task.MinDaysBefore = input.MinDaysBefore;
        task.MaxDaysBefore = input.MaxDaysBefore;
        await db.SaveChangesAsync();

        return OperationResult<ReleaseTaskDto>.Success(ToDto(task));
    }

    public async Task<OperationResult<ReleaseTaskDto>> ToggleAsync(Guid id)
    {
        var task = await db.ReleaseTasks.FindAsync(id);
        if (task is null) return OperationResult<ReleaseTaskDto>.NotFound();

        task.IsDone = !task.IsDone;
        task.CompletedAt = task.IsDone ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();

        return OperationResult<ReleaseTaskDto>.Success(ToDto(task));
    }

    public async Task<OperationResult> ReorderAsync(Guid releaseId, ReorderTasksInput input)
    {
        var tasks = await db.ReleaseTasks
            .Where(t => t.ReleaseId == releaseId && t.Phase == input.Phase)
            .ToListAsync();
        if (tasks.Count == 0) return OperationResult.NotFound();

        var applied = Reorder.TryApply(tasks, input.OrderedTaskIds, t => t.Id, (t, i) => t.SortOrder = i);
        if (!applied)
            return OperationResult.Invalid(new[] { "Reorder must list every task in the phase exactly once." });

        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        var task = await db.ReleaseTasks.FindAsync(id);
        if (task is null) return OperationResult.NotFound();

        db.ReleaseTasks.Remove(task);
        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    // Append position for a task added to (or moved into) a phase: one past the current max.
    private async Task<int> NextSortOrder(Guid releaseId, Phase phase, Guid? excludeTaskId = null) =>
        (await db.ReleaseTasks
            .Where(t => t.ReleaseId == releaseId && t.Phase == phase
                && (excludeTaskId == null || t.Id != excludeTaskId))
            .Select(t => (int?)t.SortOrder)
            .MaxAsync() ?? -1) + 1;

    private static ReleaseTaskDto ToDto(ReleaseTask t) =>
        new(t.Id, t.Title, t.Phase, t.SortOrder, t.IsDone, t.CompletedAt, t.Notes, t.MinDaysBefore, t.MaxDaysBefore);
}
