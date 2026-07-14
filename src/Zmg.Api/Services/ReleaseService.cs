using Microsoft.EntityFrameworkCore;
using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Domain.Entities;
using Zmg.Domain.Enums;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Release orchestration: list with derived status/progress, detail with phase-grouped tasks,
/// and create/edit which copy the type's template snapshot onto the release. Pure rules live in
/// Domain (<see cref="Validation"/>, <see cref="TemplateCopy"/>, <see cref="ProgressCalculator"/>,
/// <see cref="ReleaseStatus"/>, <see cref="PendingActions"/>); this loads/persists and maps to DTOs.
/// </summary>
public sealed class ReleaseService(ZmgDbContext db) : IReleaseService
{
    // List with progress counts (done/total) and derived status. Filterable.
    // scope=home returns only forward-looking active releases (releaseDate >= today) ordered nearest-first;
    // scope=archived returns only archived releases ordered releaseDate desc;
    // scope=all (default) returns active (non-archived) releases ordered releaseDate desc.
    // Removed (soft-deleted) releases are excluded everywhere by the global query filter.
    // q is a case-insensitive title search.
    public async Task<IReadOnlyList<ReleaseListItemDto>> ListAsync(
        Guid? artistId, ReleaseType? type, string? status, string? scope, string? q)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isHome = string.Equals(scope, "home", StringComparison.OrdinalIgnoreCase);
        var isArchived = string.Equals(scope, "archived", StringComparison.OrdinalIgnoreCase);

        var query = db.Releases.AsQueryable();
        if (artistId is { } aid) query = query.Where(r => r.MainArtistId == aid);
        if (type is { } t) query = query.Where(r => r.Type == t);
        // Archived releases live only in the archived scope; every other scope shows active ones.
        query = isArchived ? query.Where(r => r.ArchivedAt != null) : query.Where(r => r.ArchivedAt == null);
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
                MainArtistName = r.MainArtist!.Name, r.CoverUrl, r.Upc, r.Isrc, r.ArchivedAt,
                Done = r.Tasks.Count(x => x.IsDone),
                Total = r.Tasks.Count,
                Distributed = r.Tasks.Any(x => x.IsDone && x.Title == SeedData.DistributeToDspsTitle),
            })
            .ToListAsync();

        return rows
            .Select(r =>
            {
                var progress = new ProgressCount(r.Done, r.Total);
                return new ReleaseListItemDto(
                    r.Id, r.Title, r.Type, r.ReleaseDate, r.MainArtistId, r.MainArtistName,
                    r.CoverUrl, r.Done, r.Total,
                    ReleaseStatus.Derive(r.ReleaseDate, today, progress, r.ArchivedAt != null),
                    r.Upc, r.Isrc,
                    Release.NeedsWarning(r.Distributed, r.Upc, r.Isrc));
            })
            .Where(r => status is null || string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<OperationResult<ReleaseDetailDto>> GetAsync(Guid id)
    {
        var release = await db.Releases.WithDetailIncludes().FirstOrDefaultAsync(r => r.Id == id);
        if (release is null) return OperationResult<ReleaseDetailDto>.NotFound();

        return OperationResult<ReleaseDetailDto>.Success(ToDetail(release));
    }

    // Create; copies the default template for the type onto the release.
    public async Task<OperationResult<ReleaseDetailDto>> CreateAsync(ReleaseInput input)
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
            return OperationResult<ReleaseDetailDto>.Invalid(validation.Errors);

        var template = await LoadTemplate(input.Type);
        if (template is null)
            return OperationResult<ReleaseDetailDto>.Problem($"No template seeded for release type {input.Type}.");

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
        // also, if a release is created with ISRC and UPC, it has been already distributed.
        if (release.ReleaseDate < today 
            || (!string.IsNullOrWhiteSpace(release.Isrc) && !string.IsNullOrWhiteSpace(release.Upc)))
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

        var created = await db.Releases.WithDetailIncludes().FirstAsync(r => r.Id == release.Id);
        return OperationResult<ReleaseDetailDto>.Success(ToDetail(created), validation.Warnings);
    }

    public async Task<OperationResult<ReleaseDetailDto>> UpdateAsync(Guid id, ReleaseInput input)
    {
        var release = await db.Releases
            .Include(r => r.FeaturedArtists)
            .Include(r => r.Tasks)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (release is null) return OperationResult<ReleaseDetailDto>.NotFound();

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
            return OperationResult<ReleaseDetailDto>.Invalid(validation.Errors);

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

        var updated = await db.Releases.WithDetailIncludes().FirstAsync(r => r.Id == id);
        return OperationResult<ReleaseDetailDto>.Success(ToDetail(updated), validation.Warnings);
    }

    // Archive (v1.2): a terminal, non-restorable state. Only a release still to come (releaseDate >= today)
    // can be archived, and never twice.
    public async Task<OperationResult> ArchiveAsync(Guid id)
    {
        var release = await db.Releases.FindAsync(id);
        if (release is null) return OperationResult.NotFound();

        if (release.IsArchived)
            return OperationResult.Conflict(new[] { "Release is already archived." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (release.ReleaseDate < today)
            return OperationResult.Conflict(new[] { "Only releases dated today or later can be archived." });

        release.ArchivedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    // Remove (v1.2): a soft-delete reachable only from an archived release. Releases are never hard-deleted —
    // the row is stamped DeletedAt and hidden everywhere by the global query filter.
    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        var release = await db.Releases.FindAsync(id);
        if (release is null) return OperationResult.NotFound();

        if (!release.IsArchived)
            return OperationResult.Conflict(new[] { "Only archived releases can be removed." });

        release.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    // Optional free-text identifiers: trim, and store blank as null (no format validation, §6).
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<ChecklistTemplate?> LoadTemplate(ReleaseType type) =>
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

        return new ReleaseDetailDto(
            release.Id, release.Title, release.Type, release.ReleaseDate,
            release.MainArtistId, release.MainArtist?.Name ?? string.Empty,
            release.CoverUrl, release.Notes,
            ReleaseStatus.Derive(release.ReleaseDate, today, progress.Overall, release.IsArchived),
            featured, progress.Overall.Done, progress.Overall.Total, phases, tracks,
            release.Upc, release.Isrc,
            Release.NeedsWarning(release.IsDistributed, release.Upc, release.Isrc),
            release.IsArchived);
    }
}
