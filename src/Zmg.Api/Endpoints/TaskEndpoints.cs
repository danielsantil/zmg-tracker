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
        var taskGroup = app.MapGroup("/api/tasks").WithTags("Tasks");
        var releaseGroup = app.MapGroup("/api/releases").WithTags("Tasks");

        // Add an ad-hoc task to a release, appended to the end of its phase.
        releaseGroup.MapPost("/{releaseId:guid}/tasks", async (Guid releaseId, AddTaskInput input, IReleaseTaskService tasks, CancellationToken ct) =>
            (await tasks.AddAsync(releaseId, input, ct)).ToCreated(t => $"/api/tasks/{t.Id}"));

        // Rename / move phase / edit notes.
        taskGroup.MapPut("/{id:guid}", async (Guid id, UpdateTaskInput input, IReleaseTaskService tasks, CancellationToken ct) =>
            (await tasks.UpdateAsync(id, input, ct)).ToOk());

        // Check / uncheck, stamping CompletedAt on the transition to done.
        taskGroup.MapPatch("/{id:guid}/toggle", async (Guid id, IReleaseTaskService tasks, CancellationToken ct) =>
            (await tasks.ToggleAsync(id, ct)).ToOk());

        // Reorder tasks within a single phase; SortOrder follows the given id order.
        releaseGroup.MapPut("/{releaseId:guid}/tasks/order", async (Guid releaseId, ReorderTasksInput input, IReleaseTaskService tasks, CancellationToken ct) =>
            (await tasks.ReorderAsync(releaseId, input, ct)).ToNoContent());

        taskGroup.MapDelete("/{id:guid}", async (Guid id, IReleaseTaskService tasks, CancellationToken ct) =>
            (await tasks.DeleteAsync(id, ct)).ToNoContent());
    }
}
