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
/// Release orchestration: list with derived status/progress, detail with phase-grouped tasks and
/// its tracks (each projected from its song), and create/edit which copy the type's template snapshot
/// onto the release. Create also materialises the inline Tracks section (v2.0). Pure rules live in
/// Domain (<see cref="Validation"/>, <see cref="TemplateCopy"/>, <see cref="ProgressCalculator"/>,
/// <see cref="ReleaseStatus"/>, <see cref="ReleaseArchival"/>, <see cref="ReleaseMutability"/>,
/// <see cref="PendingActions"/>); this loads/persists and maps to DTOs.
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
        Guid? artistId, ReleaseType? type, string? status, string? scope, string? q, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isHome = string.Equals(scope, "home", StringComparison.OrdinalIgnoreCase);
        var isArchived = string.Equals(scope, "archived", StringComparison.OrdinalIgnoreCase);

        var query = db.Releases.AsNoTracking();
        if (artistId is { } aid) query = query.Where(r => r.MainArtistId == aid);
        if (type is { } t) query = query.Where(r => r.Type == t);
        // Archived releases live only in the archived scope; every other scope shows active ones.
        query = isArchived ? query.Where(r => r.ArchivedAt != null) : query.Where(r => r.ArchivedAt == null);
        if (isHome) query = query.Where(r => r.ReleaseDate >= today);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(r => EF.Functions.Like(r.Title.ToLower(), $"%{term.ToLower()}%"));
        }

        query = isHome ? query.OrderBy(r => r.ReleaseDate) : query.OrderByDescending(r => r.ReleaseDate);

        var rows = await query
            .Select(r => new
            {
                r.Id, r.Title, r.Type, r.ReleaseDate, r.MainArtistId,
                MainArtistName = r.MainArtist!.Name, r.CoverUrl, r.Upc, r.ArchivedAt,
                Done = r.Tasks.Count(x => x.IsDone),
                Total = r.Tasks.Count,
                TrackCount = r.Tracks.Count,
                Distributed = r.Tasks.Any(x => x.IsDone && x.Title == SeedData.DistributeToDspsTitle),
            })
            .ToListAsync(ct);

        return rows
            .Select(r =>
            {
                var progress = new ProgressCount(r.Done, r.Total);
                var archived = r.ArchivedAt != null;
                return new ReleaseListItemDto(
                    r.Id, r.Title, r.Type, r.ReleaseDate, r.MainArtistId, r.MainArtistName,
                    r.CoverUrl, r.Done, r.Total,
                    ReleaseStatus.Derive(r.ReleaseDate, today, progress, archived),
                    r.Upc,
                    ReleaseWarnings.Compute(r.Type, r.TrackCount, archived, r.Distributed, r.Upc),
                    ReleaseArchival.CanArchive(r.ReleaseDate, today, archived));
            })
            .Where(r => status is null || string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<OperationResult<ReleaseDetailDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var release = await db.Releases.AsNoTracking().WithDetailIncludes().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (release is null) return OperationResult<ReleaseDetailDto>.NotFound();

        return OperationResult<ReleaseDetailDto>.Success(ToDetail(release));
    }

    // Create; copies the default template for the type onto the release and materialises the inline
    // Tracks section (new songs and/or existing catalog songs). Split into steps (M25 task 6): validate,
    // resolve the existing catalog songs, then build + persist the release with its tracks.
    public async Task<OperationResult<ReleaseDetailDto>> CreateAsync(ReleaseInput input, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var trackInputs = input.Tracks ?? new List<TrackInput>();

        var validation = await ValidateCreateAsync(input, trackInputs, today, ct);
        if (!validation.IsValid)
            return OperationResult<ReleaseDetailDto>.Invalid(validation.Errors);

        var resolved = await ResolveExistingSongsAsync(trackInputs, ct);
        if (resolved is { } failure)
            return failure;

        var template = await LoadTemplate(input.Type, ct);
        if (template is null)
            return OperationResult<ReleaseDetailDto>.Problem($"No template seeded for release type {input.Type}.");

        var release = BuildRelease(input, template, today);
        MaterialiseTracks(release, trackInputs);

        db.Releases.Add(release);
        await db.SaveChangesAsync(ct);

        var created = await db.Releases.AsNoTracking().WithDetailIncludes().FirstAsync(r => r.Id == release.Id, ct);
        return OperationResult<ReleaseDetailDto>.Success(ToDetail(created), validation.Warnings);
    }

    // Structural + per-artist title rules for a create. The title-clash rule (incl. within-request
    // dedupe) is pure Domain now (Validation.ValidateReleaseTracks), fed the artist's active song titles.
    private async Task<ValidationResult> ValidateCreateAsync(
        ReleaseInput input, IReadOnlyList<TrackInput> trackInputs, DateOnly today, CancellationToken ct)
    {
        var mainArtistExists = input.MainArtistId != Guid.Empty
            && await db.Artists.AsNoTracking().AnyAsync(a => a.Id == input.MainArtistId, ct);
        var otherTitles = await db.Releases.AsNoTracking()
            .Where(r => r.MainArtistId == input.MainArtistId && r.ArchivedAt == null)
            .Select(r => r.Title)
            .ToListAsync(ct);

        var validation = Validation.ValidateRelease(
            input.Title, input.MainArtistId, mainArtistExists, input.ReleaseDate, today, otherTitles);

        var activeSongTitles = await db.Songs.AsNoTracking()
            .Where(s => s.MainArtistId == input.MainArtistId && s.ArchivedAt == null)
            .Select(s => s.Title)
            .ToListAsync(ct);
        var specs = trackInputs.Select(t => new TrackSpec(t.SongId, t.Title)).ToList();
        var trackValidation = Validation.ValidateReleaseTracks(input.Type, specs, activeSongTitles);
        foreach (var e in trackValidation.Errors) validation.Error(e);

        return validation;
    }

    // Resolve the existing catalog songs referenced by the specs (deleted ones are hidden by the filter);
    // returns a failure result if any are missing or archived, else null so the caller proceeds.
    private async Task<OperationResult<ReleaseDetailDto>?> ResolveExistingSongsAsync(
        IReadOnlyList<TrackInput> trackInputs, CancellationToken ct)
    {
        var existingIds = trackInputs
            .Where(t => t.SongId is { } id && id != Guid.Empty)
            .Select(t => t.SongId!.Value)
            .ToList();
        if (existingIds.Count == 0) return null;

        var existingSongs = await db.Songs.AsNoTracking()
            .Where(s => existingIds.Contains(s.Id))
            .Select(s => new { s.Id, s.IsArchived })
            .ToListAsync(ct);

        if (existingIds.Any(id => existingSongs.All(s => s.Id != id)))
            return OperationResult<ReleaseDetailDto>.Invalid(new[] { "One or more selected songs do not exist." });
        if (existingSongs.Any(s => s.IsArchived))
            return OperationResult<ReleaseDetailDto>.Conflict(new[] { "Can't add an archived song to a release." });

        return null;
    }

    private static Release BuildRelease(ReleaseInput input, ChecklistTemplate template, DateOnly today)
    {
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
            Tasks = TemplateCopy.CopyToRelease(template, Guid.Empty), // ReleaseId set below
        };
        foreach (var task in release.Tasks) task.ReleaseId = release.Id;

        // Backfill (v2.0 simplified): a release dated in the past was, by definition, already
        // distributed — auto-check its "Distribute to DSPs" task. Identifiers no longer imply
        // distribution (the ISRC branch moved to the song).
        if (release.ReleaseDate < today)
        {
            var distribute = release.Tasks.FirstOrDefault(t => t.Title == SeedData.DistributeToDspsTitle);
            if (distribute is not null)
            {
                distribute.IsDone = true;
                distribute.CompletedAt = DateTime.UtcNow;
            }
        }

        return release;
    }

    // Title uniqueness was enforced in validation; new inline songs can be materialised directly here.
    private void MaterialiseTracks(Release release, IReadOnlyList<TrackInput> trackInputs)
    {
        var trackNumber = 1;
        foreach (var t in trackInputs)
        {
            Guid songId;
            if (t.SongId is { } sid && sid != Guid.Empty)
            {
                songId = sid;
            }
            else
            {
                var song = SongMapping.NewSong(release.MainArtistId, t.Title!, t.Isrc, t.Artists);
                db.Songs.Add(song);
                songId = song.Id;
            }

            release.Tracks.Add(new Track
            {
                ReleaseId = release.Id,
                SongId = songId,
                TrackNumber = trackNumber++,
                IsFocusTrack = false,
            });
        }
    }

    public async Task<OperationResult<ReleaseDetailDto>> UpdateAsync(Guid id, ReleaseInput input, CancellationToken ct = default)
    {
        // Load the full detail graph once — mutating scalar fields on the tracked entity lets us map the
        // result without a second round-trip after save (M25 task 3).
        var release = await db.Releases.WithDetailIncludes().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (release is null) return OperationResult<ReleaseDetailDto>.NotFound();

        // Archived releases are terminal and read-only — the whole read side already assumes this (M25 defect 1).
        if (!ReleaseMutability.CanEdit(release.IsArchived))
            return OperationResult<ReleaseDetailDto>.Conflict(new[] { ReleaseMutability.ArchivedReadOnlyMessage });

        // Type is fixed at create (it determines the checklist template). Tracks mutate only via the
        // track endpoints, so input.Tracks is ignored on PUT.
        if (input.Type != release.Type)
            return OperationResult<ReleaseDetailDto>.Conflict(new[] { "Release type can't change after creation." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var mainArtistExists = input.MainArtistId != Guid.Empty
            && await db.Artists.AsNoTracking().AnyAsync(a => a.Id == input.MainArtistId, ct);
        var otherTitles = await db.Releases.AsNoTracking()
            .Where(r => r.MainArtistId == input.MainArtistId && r.Id != id)
            .Select(r => r.Title)
            .ToListAsync(ct);

        var validation = Validation.ValidateRelease(
            input.Title, input.MainArtistId, mainArtistExists, input.ReleaseDate, today, otherTitles);
        if (!validation.IsValid)
            return OperationResult<ReleaseDetailDto>.Invalid(validation.Errors);

        release.Title = input.Title.Trim();
        release.ReleaseDate = input.ReleaseDate!.Value;
        release.MainArtistId = input.MainArtistId;
        release.CoverUrl = string.IsNullOrWhiteSpace(input.CoverUrl) ? null : input.CoverUrl.Trim();
        release.Notes = input.Notes;
        release.Upc = Clean(input.Upc);

        await db.SaveChangesAsync(ct);

        // The edit may re-point MainArtistId; the loaded navigation still holds the old artist. Reload the
        // reference to match the new FK so ToDetail reports the right name (no full detail re-query needed).
        if (release.MainArtist?.Id != release.MainArtistId)
            await db.Entry(release).Reference(r => r.MainArtist).LoadAsync(ct);

        return OperationResult<ReleaseDetailDto>.Success(ToDetail(release), validation.Warnings);
    }

    // Preview the archive cascade (2.0 improvement): the titles of the songs that would archive alongside
    // this release, so the UI can warn before confirming. Same rule as ArchiveAsync — released songs and
    // songs shared with an active release are excluded. Read-only; nothing is persisted here.
    public async Task<OperationResult<ArchivePreviewDto>> GetArchivePreviewAsync(Guid id, CancellationToken ct = default)
    {
        // Identity-resolution no-tracking: the include path cycles back to Release (Track→Song→ReleaseLinks→
        // Release), which plain AsNoTracking rejects. Read-only, so no change tracking is wanted.
        var release = await db.Releases.AsNoTrackingWithIdentityResolution()
            .Include(r => r.Tracks).ThenInclude(t => t.Song).ThenInclude(s => s!.ReleaseLinks).ThenInclude(t => t.Release)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (release is null) return OperationResult<ArchivePreviewDto>.NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var titles = release.Tracks
            .Select(t => t.Song)
            .OfType<Song>()
            .Where(s => SongArchival.ShouldArchive(s, release.Id, today))
            .Select(s => s.Title)
            .ToList();

        return OperationResult<ArchivePreviewDto>.Success(new ArchivePreviewDto(titles));
    }

    // Archive (v1.2): a terminal, non-restorable state. Only a release still to come (releaseDate >= today)
    // can be archived, and never twice. v2.0 M15: archiving cascades to the release's exclusively-linked
    // upcoming songs (see SongArchival.ShouldArchive) — released songs and songs shared with an active
    // release stay put.
    public async Task<OperationResult> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var release = await db.Releases
            .Include(r => r.Tracks).ThenInclude(t => t.Song).ThenInclude(s => s!.ReleaseLinks).ThenInclude(t => t.Release)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (release is null) return OperationResult.NotFound();

        if (release.IsArchived)
            return OperationResult.Conflict(new[] { "Release is already archived." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (release.ReleaseDate < today)
            return OperationResult.Conflict(new[] { "Only releases dated today or later can be archived." });

        var now = DateTime.UtcNow;
        release.ArchivedAt = now;

        // Cascade: pull each exclusively-linked upcoming song into the archive alongside the release.
        foreach (var song in release.Tracks.Select(t => t.Song).OfType<Song>())
        {
            if (SongArchival.ShouldArchive(song, release.Id, today))
                song.ArchivedAt = now;
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    // Remove (v1.2): a soft-delete reachable only from an archived release. Releases are never hard-deleted —
    // the row is stamped DeletedAt and hidden everywhere by the global query filter.
    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var release = await db.Releases.FindAsync([id], ct);
        if (release is null) return OperationResult.NotFound();

        if (!release.IsArchived)
            return OperationResult.Conflict(new[] { "Only archived releases can be removed." });

        release.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    // Optional free-text identifier: trim, and store blank as null (no format validation, §6).
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<ChecklistTemplate?> LoadTemplate(ReleaseType type, CancellationToken ct) =>
        await db.ChecklistTemplates
            .Include(t => t.Tasks)
            .FirstOrDefaultAsync(t => t.Type == type, ct);

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

        var tracks = release.Tracks
            .OrderBy(t => t.TrackNumber)
            .Select(SongMapping.ToDto)
            .ToList();

        return new ReleaseDetailDto(
            release.Id, release.Title, release.Type, release.ReleaseDate,
            release.MainArtistId, release.MainArtist?.Name ?? string.Empty,
            release.CoverUrl, release.Notes,
            ReleaseStatus.Derive(release.ReleaseDate, today, progress.Overall, release.IsArchived),
            progress.Overall.Done, progress.Overall.Total, phases, tracks,
            release.Upc,
            ReleaseWarnings.Compute(release.Type, release.Tracks.Count, release.IsArchived, release.IsDistributed, release.Upc),
            release.IsArchived,
            ReleaseArchival.CanArchive(release.ReleaseDate, today, release.IsArchived));
    }
}
