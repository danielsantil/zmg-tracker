using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;

namespace Zmg.Api.Endpoints;

/// <summary>
/// Live-checklist task mutations (M2). Toggling is the daily action, so these stay
/// small and single-purpose; the frontend recomputes progress from the task list.
/// Logic lives in IReleaseTaskService.
/// </summary>
public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // Add an ad-hoc task to a release, appended to the end of its phase.
        app.MapPost("/api/releases/{releaseId:guid}/tasks", async (Guid releaseId, AddTaskInput input, IReleaseTaskService tasks) =>
            (await tasks.AddAsync(releaseId, input)).ToCreated(t => $"/api/tasks/{t.Id}"));

        // Rename / move phase / edit notes.
        app.MapPut("/api/tasks/{id:guid}", async (Guid id, UpdateTaskInput input, IReleaseTaskService tasks) =>
            (await tasks.UpdateAsync(id, input)).ToOk());

        // Check / uncheck, stamping CompletedAt on the transition to done.
        app.MapPatch("/api/tasks/{id:guid}/toggle", async (Guid id, IReleaseTaskService tasks) =>
            (await tasks.ToggleAsync(id)).ToOk());

        // Reorder tasks within a single phase; SortOrder follows the given id order.
        app.MapPut("/api/releases/{releaseId:guid}/tasks/order", async (Guid releaseId, ReorderTasksInput input, IReleaseTaskService tasks) =>
            (await tasks.ReorderAsync(releaseId, input)).ToNoContent());

        app.MapDelete("/api/tasks/{id:guid}", async (Guid id, IReleaseTaskService tasks) =>
            (await tasks.DeleteAsync(id)).ToNoContent());
    }
}
