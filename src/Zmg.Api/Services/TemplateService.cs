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
public sealed class TemplateService(ZmgDbContext db, ITaskTranslationService translations) : ITemplateService
{
    public async Task<IReadOnlyList<TemplateDto>> ListAsync(CancellationToken ct = default)
    {
        var templates = await db.ChecklistTemplates.AsNoTracking()
            .Include(t => t.Tasks)
            .OrderBy(t => t.Type)
            .ToListAsync(ct);

        var text = await translations.ForRequestLocaleAsync(ct);
        return templates.Select(t => ToDto(t, text)).ToList();
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

        return OperationResult<TemplateTaskDto>.Success(ToDto(task, await translations.ForRequestLocaleAsync(ct)));
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

        // A title edit is applied to *the locale the editor is reading* (M48). Editing a seeded task
        // while in Spanish rewrites its Spanish row and leaves the English standard — and the code —
        // alone; editing in English rewrites the standard and leaves the Spanish alone. That is what
        // makes the copy correctable in the app instead of by migration, and it supersedes M47's
        // "clear the Code on edit" for template tasks: with per-locale edits there is no silent revert
        // to protect against, and dropping the code would orphan every other locale's text.
        // (Release tasks keep M47's rule — a release owns a snapshot, with no per-locale text of its own.)
        //
        // "Edited" is still measured against what the user was shown, not the English column: the SPA
        // round-trips the title on a phase move, so comparing to `Title` would treat every Spanish move
        // as an edit.
        var text = await translations.ForRequestLocaleAsync(ct);
        var newTitle = input.Title.Trim();
        var responseTitle = TaskText.Resolve(task.Code, task.Title, text);

        if (!string.Equals(newTitle, responseTitle, StringComparison.Ordinal))
        {
            responseTitle = newTitle;
            if (task.Code is not null && translations.Locale != TaskText.DefaultLocale)
                await UpsertTranslationAsync(task, translations.Locale, newTitle, ct);
            else
                task.Title = newTitle; // English, or a user-added task that has no locale to key off
        }

        task.MinDaysBefore = input.MinDaysBefore;
        task.MaxDaysBefore = input.MaxDaysBefore;
        await db.SaveChangesAsync(ct);

        return OperationResult<TemplateTaskDto>.Success(
            new TemplateTaskDto(task.Id, responseTitle, task.Phase, task.SortOrder, task.MinDaysBefore, task.MaxDaysBefore));
    }

    /// <summary>
    /// Writes one locale's text for a task — and for <b>every</b> template task sharing its code.
    /// </summary>
    /// <remarks>
    /// Code-scoped, not row-scoped, because the lookup map is keyed by code: a release task carries a
    /// <c>SourceCode</c> and no live FK, so there is nothing else to resolve through. The base checklist
    /// is seeded into both templates, so writing only this row would leave the other one supplying the
    /// old text to the same code — the edit would appear to do nothing, which is exactly what the test
    /// for this caught. (English titles stay per-row, as they always have been: <c>Title</c> is a
    /// column, and nothing resolves it by code.)
    /// <para>
    /// Text identical to the English title <b>deletes</b> the row rather than storing it: falling back
    /// to the column is the same result with less to keep in step, and it is the rule the seed already
    /// follows for the untranslatable proper nouns.
    /// </para>
    /// </remarks>
    private async Task UpsertTranslationAsync(TemplateTask task, string locale, string text, CancellationToken ct)
    {
        var code = task.Code!;
        var siblings = await db.TemplateTasks.Where(t => t.Code == code).ToListAsync(ct);
        var existingByTask = (await db.TemplateTaskTranslations
                .Where(t => t.Locale == locale && t.TemplateTask!.Code == code)
                .ToListAsync(ct))
            .ToDictionary(t => t.TemplateTaskId);

        foreach (var sibling in siblings)
        {
            existingByTask.TryGetValue(sibling.Id, out var existing);
            Write(sibling, existing);
        }

        void Write(TemplateTask target, TemplateTaskTranslation? existing)
        {
            if (string.Equals(text, target.Title, StringComparison.Ordinal))
            {
                if (existing is not null) db.TemplateTaskTranslations.Remove(existing);
                return;
            }

            if (existing is null)
                db.TemplateTaskTranslations.Add(new TemplateTaskTranslation { TemplateTaskId = target.Id, Locale = locale, Text = text });
            else
                existing.Text = text;
        }
    }

    public async Task<OperationResult> ReorderTasksAsync(Guid templateId, ReorderTemplateTasksInput input, CancellationToken ct = default)
    {
        var tasks = await db.TemplateTasks
            .Where(t => t.ChecklistTemplateId == templateId && t.Phase == input.Phase)
            .ToListAsync(ct);
        if (tasks.Count == 0) return OperationResult.NotFound();

        var applied = Reorder.TryApply(tasks, input.OrderedTaskIds, t => t.Id, (t, i) => t.SortOrder = i);
        if (!applied)
            return OperationResult.Invalid([ServiceErrors.TaskReorderMismatch]);

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

    private static TemplateDto ToDto(ChecklistTemplate template, IReadOnlyDictionary<string, string> text)
    {
        var phases = Enum.GetValues<Phase>()
            .Select(phase => new TemplatePhaseGroupDto(
                phase,
                template.Tasks
                    .Where(t => t.Phase == phase)
                    .OrderBy(t => t.SortOrder)
                    .Select(t => ToDto(t, text))
                    .ToList()))
            .ToList();
        return new TemplateDto(template.Id, template.Type, phases);
    }

    // Title is resolved per locale at read time (M47) — the stored row never changes, so a user's edit
    // is never silently reverted and English is always the fallback. Mutation responses go through the
    // same mapper: the SPA replaces its local row with what comes back, so English there would flip the
    // list mid-edit for a Spanish reader.
    private static TemplateTaskDto ToDto(TemplateTask t, IReadOnlyDictionary<string, string> text) =>
        new(t.Id, TaskText.Resolve(t.Code, t.Title, text), t.Phase, t.SortOrder, t.MinDaysBefore, t.MaxDaysBefore);

}
