using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Data;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Api.Endpoints;

public static class ReleaseEndpoints
{
    public static void MapReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/releases");

        // List with progress counts (done/total) and derived status. Filterable.
        // scope=home returns only forward-looking releases (releaseDate >= today) ordered nearest-first;
        // scope=all (default) returns everything ordered releaseDate desc. q is a case-insensitive title search.
        group.MapGet("", async (Guid? artistId, ReleaseType? type, string? status, string? scope, string? q, ZmgDbContext db) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var isHome = string.Equals(scope, "home", StringComparison.OrdinalIgnoreCase);

            var query = db.Releases.AsQueryable();
            if (artistId is { } aid) query = query.Where(r => r.MainArtistId == aid);
            if (type is { } t) query = query.Where(r => r.Type == t);
            if (isHome) query = query.Where(r => r.ReleaseDate >= today);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(r => EF.Functions.Like(r.Title, $"%{term}%"));
            }

            query = isHome ? query.OrderBy(r => r.ReleaseDate) : query.OrderByDescending(r => r.ReleaseDate);

            var rows = await query
                .Select(r => new
                {
                    r.Id, r.Title, r.Type, r.ReleaseDate, r.MainArtistId,
                    MainArtistName = r.MainArtist!.Name, r.CoverUrl, r.Upc, r.Isrc,
                    Done = r.Tasks.Count(x => x.IsDone),
                    Total = r.Tasks.Count,
                    Distributed = r.Tasks.Any(x => x.IsDone && x.Title == SeedData.DistributeToDspsTitle),
                })
                .ToListAsync();

            var items = rows
                .Select(r =>
                {
                    var progress = new ProgressCount(r.Done, r.Total);
                    return new ReleaseListItemDto(
                        r.Id, r.Title, r.Type, r.ReleaseDate, r.MainArtistId, r.MainArtistName,
                        r.CoverUrl, r.Done, r.Total,
                        ReleaseStatus.Derive(r.ReleaseDate, today, progress),
                        r.Upc, r.Isrc,
                        Release.NeedsWarning(r.Distributed, r.Upc, r.Isrc));
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
                Upc = Clean(input.Upc),
                Isrc = Clean(input.Isrc),
                Tasks = TemplateCopy.CopyToRelease(template, Guid.Empty), // ReleaseId set below
            };
            foreach (var task in release.Tasks) task.ReleaseId = release.Id;

            // Backfill (1.0 §9): a release dated in the past was, by definition, already distributed —
            // auto-check its "Distribute to DSPs" task so a blank UPC/ISRC surfaces as a pending action (M10).
            if (release.ReleaseDate < today)
            {
                var distribute = release.Tasks.FirstOrDefault(t => t.Title == SeedData.DistributeToDspsTitle);
                if (distribute is not null)
                {
                    distribute.IsDone = true;
                    distribute.CompletedAt = DateTime.UtcNow;
                }
            }

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
            release.Upc = Clean(input.Upc);
            release.Isrc = Clean(input.Isrc);

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

    // Optional free-text identifiers: trim, and store blank as null (no format validation, §6).
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
                        t.Id, t.Title, t.Phase, t.SortOrder, t.IsDone, t.CompletedAt, t.Notes,
                        t.MinDaysBefore, t.MaxDaysBefore))
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

        var distributed = Release.IsDistributed(release.Tasks);
        var pending = PendingActions.Compute(release, release.Tasks, today);

        return new ReleaseDetailDto(
            release.Id, release.Title, release.Type, release.ReleaseDate,
            release.MainArtistId, release.MainArtist?.Name ?? string.Empty,
            release.CoverUrl, release.Notes,
            ReleaseStatus.Derive(release.ReleaseDate, today, progress.Overall),
            featured, progress.Overall.Done, progress.Overall.Total, phases, tracks,
            release.Upc, release.Isrc,
            Release.NeedsWarning(distributed, release.Upc, release.Isrc),
            pending);
    }
}
