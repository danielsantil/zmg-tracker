using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Data;
using Zmg.Domain;

namespace Zmg.Api.Endpoints;

/// <summary>
/// Template management (M3). Edits here only shape *future* releases — existing releases
/// own a snapshot copy taken on create, so nothing done here touches live checklists.
/// </summary>
public static class TemplateEndpoints
{
    public static void MapTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        // Both templates with their tasks, grouped by phase.
        app.MapGet("/api/templates", async (ZmgDbContext db) =>
        {
            var templates = await db.ChecklistTemplates
                .Include(t => t.Tasks)
                .OrderBy(t => t.Type)
                .ToListAsync();

            return Results.Ok(templates.Select(ToDto).ToList());
        });

        // Add a template task, appended to the end of its phase.
        app.MapPost("/api/templates/{templateId:guid}/tasks", async (Guid templateId, AddTemplateTaskInput input, ZmgDbContext db) =>
        {
            if (!await db.ChecklistTemplates.AnyAsync(t => t.Id == templateId))
                return Results.NotFound();

            var validation = Validation.ValidateTaskTitle(input.Title);
            if (!validation.IsValid)
                return Results.BadRequest(new ValidationErrorResponse(validation.Errors.ToArray()));

            var nextOrder = await db.TemplateTasks
                .Where(t => t.ChecklistTemplateId == templateId && t.Phase == input.Phase)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync() ?? -1;

            var task = new TemplateTask
            {
                Id = Guid.NewGuid(),
                ChecklistTemplateId = templateId,
                Title = input.Title.Trim(),
                Phase = input.Phase,
                SortOrder = nextOrder + 1,
                MinDaysBefore = input.MinDaysBefore,
                MaxDaysBefore = input.MaxDaysBefore,
            };
            db.TemplateTasks.Add(task);
            await db.SaveChangesAsync();

            return Results.Created($"/api/template-tasks/{task.Id}", ToDto(task));
        });

        // Rename / move phase.
        app.MapPut("/api/template-tasks/{id:guid}", async (Guid id, UpdateTemplateTaskInput input, ZmgDbContext db) =>
        {
            var task = await db.TemplateTasks.FindAsync(id);
            if (task is null) return Results.NotFound();

            var validation = Validation.ValidateTaskTitle(input.Title);
            if (!validation.IsValid)
                return Results.BadRequest(new ValidationErrorResponse(validation.Errors.ToArray()));

            // Moving to a new phase appends to the end of the target phase.
            if (task.Phase != input.Phase)
            {
                var nextOrder = await db.TemplateTasks
                    .Where(t => t.ChecklistTemplateId == task.ChecklistTemplateId && t.Phase == input.Phase && t.Id != id)
                    .Select(t => (int?)t.SortOrder)
                    .MaxAsync() ?? -1;
                task.Phase = input.Phase;
                task.SortOrder = nextOrder + 1;
            }

            task.Title = input.Title.Trim();
            task.MinDaysBefore = input.MinDaysBefore;
            task.MaxDaysBefore = input.MaxDaysBefore;
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(task));
        });

        // Reorder template tasks within a single phase; SortOrder follows the given id order.
        app.MapPut("/api/templates/{templateId:guid}/tasks/order", async (Guid templateId, ReorderTemplateTasksInput input, ZmgDbContext db) =>
        {
            var tasks = await db.TemplateTasks
                .Where(t => t.ChecklistTemplateId == templateId && t.Phase == input.Phase)
                .ToListAsync();
            if (tasks.Count == 0) return Results.NotFound();

            var byId = tasks.ToDictionary(t => t.Id);
            if (input.OrderedTaskIds.Count != tasks.Count || input.OrderedTaskIds.Any(tid => !byId.ContainsKey(tid)))
                return Results.BadRequest(new ValidationErrorResponse(
                    new[] { "Reorder must list every task in the phase exactly once." }));

            for (var i = 0; i < input.OrderedTaskIds.Count; i++)
                byId[input.OrderedTaskIds[i]].SortOrder = i;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // Delete a template task; a template must keep at least one task (§6).
        app.MapDelete("/api/template-tasks/{id:guid}", async (Guid id, ZmgDbContext db) =>
        {
            var task = await db.TemplateTasks.FindAsync(id);
            if (task is null) return Results.NotFound();

            var remaining = await db.TemplateTasks
                .CountAsync(t => t.ChecklistTemplateId == task.ChecklistTemplateId && t.Id != id);
            var validation = Validation.ValidateTemplateTaskDelete(remaining);
            if (!validation.IsValid)
                return Results.Conflict(new ValidationErrorResponse(validation.Errors.ToArray()));

            db.TemplateTasks.Remove(task);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

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
