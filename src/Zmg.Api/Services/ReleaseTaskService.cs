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
/// Every write is gated on <see cref="ReleaseMutability"/> — archived releases are read-only (M25).
/// </summary>
public sealed class ReleaseTaskService(ZmgDbContext db, ITaskTranslationService translations) : IReleaseTaskService
{
    public async Task<OperationResult<ReleaseTaskDto>> AddAsync(Guid releaseId, AddTaskInput input, CancellationToken ct = default)
    {
        var archived = await db.Releases.AsNoTracking()
            .Where(r => r.Id == releaseId)
            .Select(r => (bool?)(r.ArchivedAt != null))
            .FirstOrDefaultAsync(ct);
        if (archived is null) return OperationResult<ReleaseTaskDto>.NotFound();
        if (!ReleaseMutability.CanEdit(archived.Value))
            return OperationResult<ReleaseTaskDto>.Conflict([ReleaseMutability.ArchivedReadOnlyCode]);

        var validation = Validation.ValidateTaskTitle(input.Title);
        if (!validation.IsValid)
            return OperationResult<ReleaseTaskDto>.Invalid(validation.Errors);

        var task = new ReleaseTask
        {
            Id = Guid.NewGuid(),
            ReleaseId = releaseId,
            Title = input.Title.Trim(),
            Phase = input.Phase,
            SortOrder = await NextSortOrder(releaseId, input.Phase, ct: ct),
            IsDone = false,
            MinDaysBefore = input.MinDaysBefore,
            MaxDaysBefore = input.MaxDaysBefore,
        };
        db.ReleaseTasks.Add(task);
        await db.SaveChangesAsync(ct);

        return OperationResult<ReleaseTaskDto>.Success(ToDto(task, await translations.ForRequestLocaleAsync(ct)));
    }

    public async Task<OperationResult<ReleaseTaskDto>> UpdateAsync(Guid id, UpdateTaskInput input, CancellationToken ct = default)
    {
        var task = await db.ReleaseTasks.FindAsync([id], ct);
        if (task is null) return OperationResult<ReleaseTaskDto>.NotFound();
        if (await IsArchived(task.ReleaseId, ct))
            return OperationResult<ReleaseTaskDto>.Conflict([ReleaseMutability.ArchivedReadOnlyCode]);

        var validation = Validation.ValidateTaskTitle(input.Title);
        if (!validation.IsValid)
            return OperationResult<ReleaseTaskDto>.Invalid(validation.Errors);

        // Moving to a new phase appends to the end of the target phase.
        if (task.Phase != input.Phase)
        {
            task.SortOrder = await NextSortOrder(task.ReleaseId, input.Phase, excludeTaskId: id, ct: ct);
            task.Phase = input.Phase;
        }

        // A real title edit makes the task custom: store the new text and drop the code, so a language
        // switch can never silently revert what the user typed (M47).
        //
        // "Real" is measured against the text they were *shown*, not the stored English one. The SPA
        // sends the whole editable row back on any edit — a phase move round-trips the title verbatim —
        // so comparing against the column would let a Spanish reader's phase move overwrite the English
        // title with its own translation and orphan the code, for every task, silently.
        var text = await translations.ForRequestLocaleAsync(ct);
        var newTitle = input.Title.Trim();
        if (!string.Equals(newTitle, TaskText.Resolve(task.SourceCode, task.Title, text), StringComparison.Ordinal))
        {
            task.Title = newTitle;
            task.SourceCode = null;
        }

        task.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        task.MinDaysBefore = input.MinDaysBefore;
        task.MaxDaysBefore = input.MaxDaysBefore;
        await db.SaveChangesAsync(ct);

        return OperationResult<ReleaseTaskDto>.Success(ToDto(task, text));
    }

    public async Task<OperationResult<ReleaseTaskDto>> ToggleAsync(Guid id, CancellationToken ct = default)
    {
        var task = await db.ReleaseTasks.FindAsync([id], ct);
        if (task is null) return OperationResult<ReleaseTaskDto>.NotFound();
        if (await IsArchived(task.ReleaseId, ct))
            return OperationResult<ReleaseTaskDto>.Conflict([ReleaseMutability.ArchivedReadOnlyCode]);

        task.IsDone = !task.IsDone;
        task.CompletedAt = task.IsDone ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);

        return OperationResult<ReleaseTaskDto>.Success(ToDto(task, await translations.ForRequestLocaleAsync(ct)));
    }

    public async Task<OperationResult> ReorderAsync(Guid releaseId, ReorderTasksInput input, CancellationToken ct = default)
    {
        var tasks = await db.ReleaseTasks
            .Where(t => t.ReleaseId == releaseId && t.Phase == input.Phase)
            .ToListAsync(ct);
        if (tasks.Count == 0) return OperationResult.NotFound();
        if (await IsArchived(releaseId, ct))
            return OperationResult.Conflict([ReleaseMutability.ArchivedReadOnlyCode]);

        var applied = Reorder.TryApply(tasks, input.OrderedTaskIds, t => t.Id, (t, i) => t.SortOrder = i);
        if (!applied)
            return OperationResult.Invalid([ServiceErrors.TaskReorderMismatch]);

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await db.ReleaseTasks.FindAsync([id], ct);
        if (task is null) return OperationResult.NotFound();
        if (await IsArchived(task.ReleaseId, ct))
            return OperationResult.Conflict([ReleaseMutability.ArchivedReadOnlyCode]);

        db.ReleaseTasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    private Task<bool> IsArchived(Guid releaseId, CancellationToken ct) =>
        db.Releases.AsNoTracking().AnyAsync(r => r.Id == releaseId && r.ArchivedAt != null, ct);

    // Append position for a task added to (or moved into) a phase: one past the current max.
    private async Task<int> NextSortOrder(Guid releaseId, Phase phase, Guid? excludeTaskId = null, CancellationToken ct = default) =>
        (await db.ReleaseTasks
            .Where(t => t.ReleaseId == releaseId && t.Phase == phase
                && (excludeTaskId == null || t.Id != excludeTaskId))
            .Select(t => (int?)t.SortOrder)
            .MaxAsync(ct) ?? -1) + 1;

    // Mutation responses replace the row in the SPA's local state, so they must answer in the request's
    // locale (M47) — otherwise toggling a task while reading Spanish would flip its title to English.
    // After a title edit SourceCode is null, so this correctly echoes the user's own text.
    private static ReleaseTaskDto ToDto(ReleaseTask t, IReadOnlyDictionary<string, string> text) =>
        new(t.Id, TaskText.Resolve(t.SourceCode, t.Title, text), t.Phase, t.SortOrder,
            t.IsDone, t.CompletedAt, t.Notes, t.MinDaysBefore, t.MaxDaysBefore);
}
