using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Data;
using Zmg.Domain;

namespace Zmg.Api.Endpoints;

public static class ReleaseEndpoints
{
    public static void MapReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/releases");

        // List with progress counts (done/total) and derived status. Filterable.
        group.MapGet("", async (Guid? artistId, ReleaseType? type, string? status, ZmgDbContext db) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = db.Releases.AsQueryable();
            if (artistId is { } aid) query = query.Where(r => r.MainArtistId == aid);
            if (type is { } t) query = query.Where(r => r.Type == t);

            var rows = await query
                .OrderBy(r => r.ReleaseDate)
                .Select(r => new
                {
                    r.Id, r.Title, r.Type, r.ReleaseDate, r.MainArtistId,
                    MainArtistName = r.MainArtist!.Name, r.CoverUrl,
                    Done = r.Tasks.Count(x => x.IsDone),
                    Total = r.Tasks.Count,
                })
                .ToListAsync();

            var items = rows
                .Select(r =>
                {
                    var progress = new ProgressCount(r.Done, r.Total);
                    return new ReleaseListItemDto(
                        r.Id, r.Title, r.Type, r.ReleaseDate, r.MainArtistId, r.MainArtistName,
                        r.CoverUrl, r.Done, r.Total,
                        ReleaseStatus.Derive(r.ReleaseDate, today, progress));
                })
                .Where(r => status is null || string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Results.Ok(items);
        });

        // Detail with tasks grouped by phase.
        group.MapGet("/{id:guid}", async (Guid id, ZmgDbContext db) =>
        {
            var release = await db.Releases
                .Include(r => r.MainArtist)
                .Include(r => r.FeaturedArtists).ThenInclude(fa => fa.Artist)
                .Include(r => r.Tasks)
                .Include(r => r.Tracks)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (release is null) return Results.NotFound();

            return Results.Ok(ToDetail(release));
        });

        // Create; copies the default template for the type onto the release.
        group.MapPost("", async (ReleaseInput input, ZmgDbContext db) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var mainArtistExists = input.MainArtistId != Guid.Empty
                && await db.Artists.AnyAsync(a => a.Id == input.MainArtistId);
            var otherTitles = await db.Releases
                .Where(r => r.MainArtistId == input.MainArtistId)
                .Select(r => r.Title)
                .ToListAsync();

            var validation = Validation.ValidateRelease(
                input.Title, input.MainArtistId, mainArtistExists, input.ReleaseDate, today, otherTitles);
            if (!validation.IsValid)
                return Results.BadRequest(new ValidationErrorResponse(validation.Errors.ToArray()));

            var template = await LoadTemplate(db, input.Type);
            if (template is null)
                return Results.Problem($"No template seeded for release type {input.Type}.");

            var release = new Release
            {
                Id = Guid.NewGuid(),
                Title = input.Title.Trim(),
                Type = input.Type,
                ReleaseDate = input.ReleaseDate!.Value,
                MainArtistId = input.MainArtistId,
                CoverUrl = string.IsNullOrWhiteSpace(input.CoverUrl) ? null : input.CoverUrl.Trim(),
                Notes = input.Notes,
                Tasks = TemplateCopy.CopyToRelease(template, Guid.Empty), // ReleaseId set below
            };
            foreach (var task in release.Tasks) task.ReleaseId = release.Id;

            AddFeaturedArtists(release, input.FeaturedArtists);

            db.Releases.Add(release);
            await db.SaveChangesAsync();

            var created = await db.Releases
                .Include(r => r.MainArtist)
                .Include(r => r.FeaturedArtists).ThenInclude(fa => fa.Artist)
                .Include(r => r.Tasks)
                .Include(r => r.Tracks)
                .FirstAsync(r => r.Id == release.Id);

            var detail = ToDetail(created);
            return Results.Created($"/api/releases/{release.Id}",
                new CreatedWithWarnings<ReleaseDetailDto>(detail, validation.Warnings.ToArray()));
        });

        group.MapPut("/{id:guid}", async (Guid id, ReleaseInput input, ZmgDbContext db) =>
        {
            var release = await db.Releases
                .Include(r => r.FeaturedArtists)
                .Include(r => r.Tasks)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (release is null) return Results.NotFound();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var mainArtistExists = input.MainArtistId != Guid.Empty
                && await db.Artists.AnyAsync(a => a.Id == input.MainArtistId);
            var otherTitles = await db.Releases
                .Where(r => r.MainArtistId == input.MainArtistId && r.Id != id)
                .Select(r => r.Title)
                .ToListAsync();

            var validation = Validation.ValidateRelease(
                input.Title, input.MainArtistId, mainArtistExists, input.ReleaseDate, today, otherTitles);
            if (!validation.IsValid)
                return Results.BadRequest(new ValidationErrorResponse(validation.Errors.ToArray()));

            release.Title = input.Title.Trim();
            release.Type = input.Type; // note: does not re-copy the checklist
            release.ReleaseDate = input.ReleaseDate!.Value;
            release.MainArtistId = input.MainArtistId;
            release.CoverUrl = string.IsNullOrWhiteSpace(input.CoverUrl) ? null : input.CoverUrl.Trim();
            release.Notes = input.Notes;

            release.FeaturedArtists.Clear();
            AddFeaturedArtists(release, input.FeaturedArtists);

            await db.SaveChangesAsync();

            var updated = await db.Releases
                .Include(r => r.MainArtist)
                .Include(r => r.FeaturedArtists).ThenInclude(fa => fa.Artist)
                .Include(r => r.Tasks)
                .Include(r => r.Tracks)
                .FirstAsync(r => r.Id == id);

            return Results.Ok(new CreatedWithWarnings<ReleaseDetailDto>(ToDetail(updated), validation.Warnings.ToArray()));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ZmgDbContext db) =>
        {
            var release = await db.Releases.FindAsync(id);
            if (release is null) return Results.NotFound();
            db.Releases.Remove(release);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<ChecklistTemplate?> LoadTemplate(ZmgDbContext db, ReleaseType type) =>
        await db.ChecklistTemplates
            .Include(t => t.Tasks)
            .FirstOrDefaultAsync(t => t.Type == type);

    private static void AddFeaturedArtists(Release release, List<ReleaseArtistInput>? featured)
    {
        if (featured is null) return;
        foreach (var f in featured.Where(f => f.ArtistId != release.MainArtistId).DistinctBy(f => f.ArtistId))
        {
            release.FeaturedArtists.Add(new ReleaseArtist
            {
                ReleaseId = release.Id,
                ArtistId = f.ArtistId,
                Role = f.Role,
            });
        }
    }

    private static ReleaseDetailDto ToDetail(Release release)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var progress = ProgressCalculator.Calculate(release.Tasks);

        var phases = Enum.GetValues<Phase>()
            .Select(phase =>
            {
                var count = progress.ByPhase[phase];
                var tasks = release.Tasks
                    .Where(t => t.Phase == phase)
                    .OrderBy(t => t.SortOrder)
                    .Select(t => new ReleaseTaskDto(
                        t.Id, t.Title, t.Phase, t.SortOrder, t.IsDone, t.CompletedAt, t.Notes))
                    .ToList();
                return new PhaseGroupDto(phase, count.Done, count.Total, tasks);
            })
            .ToList();

        var featured = release.FeaturedArtists
            .Select(fa => new FeaturedArtistDto(fa.ArtistId, fa.Artist?.Name ?? string.Empty, fa.Role))
            .ToList();

        var tracks = release.Tracks
            .OrderBy(t => t.TrackNumber)
            .Select(t => new TrackDto(t.Id, t.TrackNumber, t.Title, t.IsFocusTrack))
            .ToList();

        return new ReleaseDetailDto(
            release.Id, release.Title, release.Type, release.ReleaseDate,
            release.MainArtistId, release.MainArtist?.Name ?? string.Empty,
            release.CoverUrl, release.Notes,
            ReleaseStatus.Derive(release.ReleaseDate, today, progress.Overall),
            featured, progress.Overall.Done, progress.Overall.Total, phases, tracks);
    }
}
