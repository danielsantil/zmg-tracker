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
public sealed class ReleaseTaskService(ZmgDbContext db) : IReleaseTaskService
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

        var validation = Validation.ValidateTaskTitle(input.TitleEn);
        if (!validation.IsValid)
            return OperationResult<ReleaseTaskDto>.Invalid(validation.Errors);

        var task = new ReleaseTask
        {
            Id = Guid.NewGuid(),
            ReleaseId = releaseId,
            TitleEn = input.TitleEn.Trim(),
            TitleEs = Clean(input.TitleEs),
            Phase = input.Phase,
            SortOrder = await NextSortOrder(releaseId, input.Phase, ct: ct),
            IsDone = false,
            MinDaysBefore = input.MinDaysBefore,
            MaxDaysBefore = input.MaxDaysBefore,
        };
        db.ReleaseTasks.Add(task);
        await db.SaveChangesAsync(ct);

        return OperationResult<ReleaseTaskDto>.Success(ToDto(task));
    }

    public async Task<OperationResult<ReleaseTaskDto>> UpdateAsync(Guid id, UpdateTaskInput input, CancellationToken ct = default)
    {
        var task = await db.ReleaseTasks.FindAsync([id], ct);
        if (task is null) return OperationResult<ReleaseTaskDto>.NotFound();
        if (await IsArchived(task.ReleaseId, ct))
            return OperationResult<ReleaseTaskDto>.Conflict([ReleaseMutability.ArchivedReadOnlyCode]);

        var validation = Validation.ValidateTaskTitle(input.TitleEn);
        if (!validation.IsValid)
            return OperationResult<ReleaseTaskDto>.Invalid(validation.Errors);

        // Moving to a new phase appends to the end of the target phase.
        if (task.Phase != input.Phase)
        {
            task.SortOrder = await NextSortOrder(task.ReleaseId, input.Phase, excludeTaskId: id, ct: ct);
            task.Phase = input.Phase;
        }

        // A plain field write, in both languages (v2.9) — and note what is *not* here: SourceCode is no
        // longer cleared. While the code doubled as a translation key it had to be, which meant
        // rewording "Distribute to DSPs" on a release silently switched off IsDistributed, and with it
        // the missing-UPC warning, the pending engine and the past-date backfill. Text and identity are
        // separate now, so a user can word their checklist however they like and the rules still hold.
        task.TitleEn = input.TitleEn.Trim();
        task.TitleEs = Clean(input.TitleEs);

        task.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        task.MinDaysBefore = input.MinDaysBefore;
        task.MaxDaysBefore = input.MaxDaysBefore;
        await db.SaveChangesAsync(ct);

        return OperationResult<ReleaseTaskDto>.Success(ToDto(task));
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

        return OperationResult<ReleaseTaskDto>.Success(ToDto(task));
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

    // Both texts as stored — the SPA replaces its local row with this and picks the column matching
    // what the reader has selected, so no mutation can flip a title's language.
    private static ReleaseTaskDto ToDto(ReleaseTask t) =>
        new(t.Id, t.TitleEn, t.TitleEs, t.Phase, t.SortOrder,
            t.IsDone, t.CompletedAt, t.Notes, t.MinDaysBefore, t.MaxDaysBefore);

    // Blank Spanish is stored as null — "show the English" is one state, not two.
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
