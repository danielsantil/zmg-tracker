using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;

namespace Zmg.Api.Endpoints;

/// <summary>
/// Template management (M3). Edits here only shape *future* releases — existing releases
/// own a snapshot copy taken on create, so nothing done here touches live checklists.
/// Logic lives in ITemplateService.
/// </summary>
public static class TemplateEndpoints
{
    public static void MapTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/templates").WithTags("Templates");
        var tasksGroup = app.MapGroup("/api/template-tasks").WithTags("Templates");

        // Both templates with their tasks, grouped by phase.
        group.MapGet("", async (ITemplateService templates, CancellationToken ct) =>
            Results.Ok(await templates.ListAsync(ct)));

        // Add a template task, appended to the end of its phase.
        group.MapPost("/{templateId:guid}/tasks", async (Guid templateId, AddTemplateTaskInput input, ITemplateService templates, CancellationToken ct) =>
            (await templates.AddTaskAsync(templateId, input, ct)).ToCreated(t => $"/api/template-tasks/{t.Id}"));

        // Rename / move phase.
        tasksGroup.MapPut("/{id:guid}", async (Guid id, UpdateTemplateTaskInput input, ITemplateService templates, CancellationToken ct) =>
            (await templates.UpdateTaskAsync(id, input, ct)).ToOk());

        // Reorder template tasks within a single phase; SortOrder follows the given id order.
        group.MapPut("/{templateId:guid}/tasks/order", async (Guid templateId, ReorderTemplateTasksInput input, ITemplateService templates, CancellationToken ct) =>
            (await templates.ReorderTasksAsync(templateId, input, ct)).ToNoContent());

        // Delete a template task; a template must keep at least one task (§6).
        tasksGroup.MapDelete("/{id:guid}", async (Guid id, ITemplateService templates, CancellationToken ct) =>
            (await templates.DeleteTaskAsync(id, ct)).ToNoContent());
    }
}
