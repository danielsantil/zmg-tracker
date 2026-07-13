using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Infra.Data;

namespace Zmg.Api.Endpoints;

/// <summary>
/// Live-checklist task mutations (M2). Toggling is the daily action, so these stay
/// small and single-purpose; the frontend recomputes progress from the task list.
/// </summary>
public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // Add an ad-hoc task to a release, appended to the end of its phase.
        app.MapPost("/api/releases/{releaseId:guid}/tasks", async (Guid releaseId, AddTaskInput input, ZmgDbContext db) =>
        {
            if (!await db.Releases.AnyAsync(r => r.Id == releaseId))
                return Results.NotFound();

            var validation = Validation.ValidateTaskTitle(input.Title);
            if (!validation.IsValid)
                return Results.BadRequest(new ValidationErrorResponse(validation.Errors.ToArray()));

            var nextOrder = await db.ReleaseTasks
                .Where(t => t.ReleaseId == releaseId && t.Phase == input.Phase)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync() ?? -1;

            var task = new ReleaseTask
            {
                Id = Guid.NewGuid(),
                ReleaseId = releaseId,
                Title = input.Title.Trim(),
                Phase = input.Phase,
                SortOrder = nextOrder + 1,
                IsDone = false,
                MinDaysBefore = input.MinDaysBefore,
                MaxDaysBefore = input.MaxDaysBefore,
            };
            db.ReleaseTasks.Add(task);
            await db.SaveChangesAsync();

            return Results.Created($"/api/tasks/{task.Id}", ToDto(task));
        });

        // Rename / move phase / edit notes.
        app.MapPut("/api/tasks/{id:guid}", async (Guid id, UpdateTaskInput input, ZmgDbContext db) =>
        {
            var task = await db.ReleaseTasks.FindAsync(id);
            if (task is null) return Results.NotFound();

            var validation = Validation.ValidateTaskTitle(input.Title);
            if (!validation.IsValid)
                return Results.BadRequest(new ValidationErrorResponse(validation.Errors.ToArray()));

            // Moving to a new phase appends to the end of the target phase.
            if (task.Phase != input.Phase)
            {
                var nextOrder = await db.ReleaseTasks
                    .Where(t => t.ReleaseId == task.ReleaseId && t.Phase == input.Phase && t.Id != id)
                    .Select(t => (int?)t.SortOrder)
                    .MaxAsync() ?? -1;
                task.Phase = input.Phase;
                task.SortOrder = nextOrder + 1;
            }

            task.Title = input.Title.Trim();
            task.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
            task.MinDaysBefore = input.MinDaysBefore;
            task.MaxDaysBefore = input.MaxDaysBefore;
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(task));
        });

        // Check / uncheck, stamping CompletedAt on the transition to done.
        app.MapPatch("/api/tasks/{id:guid}/toggle", async (Guid id, ZmgDbContext db) =>
        {
            var task = await db.ReleaseTasks.FindAsync(id);
            if (task is null) return Results.NotFound();

            task.IsDone = !task.IsDone;
            task.CompletedAt = task.IsDone ? DateTime.UtcNow : null;
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(task));
        });

        // Reorder tasks within a single phase; SortOrder follows the given id order.
        app.MapPut("/api/releases/{releaseId:guid}/tasks/order", async (Guid releaseId, ReorderTasksInput input, ZmgDbContext db) =>
        {
            var tasks = await db.ReleaseTasks
                .Where(t => t.ReleaseId == releaseId && t.Phase == input.Phase)
                .ToListAsync();
            if (tasks.Count == 0) return Results.NotFound();

            var byId = tasks.ToDictionary(t => t.Id);
            // Every id in the phase must appear exactly once in the request.
            if (input.OrderedTaskIds.Count != tasks.Count || input.OrderedTaskIds.Any(tid => !byId.ContainsKey(tid)))
                return Results.BadRequest(new ValidationErrorResponse(
                    new[] { "Reorder must list every task in the phase exactly once." }));

            for (var i = 0; i < input.OrderedTaskIds.Count; i++)
                byId[input.OrderedTaskIds[i]].SortOrder = i;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        app.MapDelete("/api/tasks/{id:guid}", async (Guid id, ZmgDbContext db) =>
        {
            var task = await db.ReleaseTasks.FindAsync(id);
            if (task is null) return Results.NotFound();
            db.ReleaseTasks.Remove(task);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static ReleaseTaskDto ToDto(ReleaseTask t) =>
        new(t.Id, t.Title, t.Phase, t.SortOrder, t.IsDone, t.CompletedAt, t.Notes, t.MinDaysBefore, t.MaxDaysBefore);
}
