using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Contracts;

// ---- Artists ----
public record ArtistDto(Guid Id, string Name, string? Notes, int ReleaseCount);
public record ArtistInput(string Name, string? Notes);

// ---- Songs / artists on songs (v2.0) ----
public record SongArtistInput(Guid ArtistId, ArtistRole Role);
public record SongArtistDto(Guid ArtistId, string Name, ArtistRole Role);

// ---- Catalog (M13) ----
// ReleaseDate = earliest non-archived linked release date, null for orphans/unreleased.
// CanArchive/IsOrphan (M15) drive the catalog row action from backend truth: Archive when CanArchive,
// Delete when IsOrphan (never released).
public record SongListItemDto(
    Guid Id,
    string Title,
    Guid MainArtistId,
    string MainArtistName,
    DateOnly? ReleaseDate,
    string? Isrc,
    int ReleaseCount,
    bool IsArchived,
    bool CanArchive,
    bool IsOrphan);

// One linked release on a song's detail. MainArtistId/Name drive the client-side artist-drift hint.
public record SongReleaseLinkDto(
    Guid ReleaseId,
    string Title,
    ReleaseType Type,
    DateOnly ReleaseDate,
    string? Upc,
    Guid MainArtistId,
    string MainArtistName,
    bool IsArchived);

public record SongDetailDto(
    Guid Id,
    string Title,
    Guid MainArtistId,
    string MainArtistName,
    string? Isrc,
    bool IsArchived,
    List<SongArtistDto> Artists,
    List<SongReleaseLinkDto> Releases);

public record SongUpdateInput(string Title, Guid MainArtistId, string? Isrc, List<SongArtistInput>? Artists);

// Create a catalog song directly (not through a release). Starts as an orphan (no release links).
public record SongCreateInput(string Title, Guid MainArtistId, string? Isrc, List<SongArtistInput>? Artists);

// ---- Releases ----
// Tracks is create-only (ignored on PUT). Exactly one of SongId/Title per track.
public record ReleaseInput(
    string Title,
    ReleaseType Type,
    DateOnly? ReleaseDate,
    Guid MainArtistId,
    string? CoverUrl,
    string? Notes,
    List<TrackInput>? Tracks,
    string? Upc = null);

public record ReleaseListItemDto(
    Guid Id,
    string Title,
    ReleaseType Type,
    DateOnly ReleaseDate,
    Guid MainArtistId,
    string MainArtistName,
    string? CoverUrl,
    int DoneTasks,
    int TotalTasks,
    string Status,
    string? Upc,
    bool NeedsIdentifierWarning,
    bool IsEmptyAlbum);

public record ReleaseDetailDto(
    Guid Id,
    string Title,
    ReleaseType Type,
    DateOnly ReleaseDate,
    Guid MainArtistId,
    string MainArtistName,
    string? CoverUrl,
    string? Notes,
    string Status,
    int DoneTasks,
    int TotalTasks,
    List<PhaseGroupDto> Phases,
    List<TrackDto> Tracks,
    string? Upc,
    bool NeedsIdentifierWarning,
    bool IsEmptyAlbum,
    bool IsArchived);

// Titles of the songs that would cascade-archive alongside this release (M15 cascade preview).
public record ArchivePreviewDto(List<string> SongsToArchive);

public record PhaseGroupDto(Phase Phase, int Done, int Total, List<ReleaseTaskDto> Tasks);

public record ReleaseTaskDto(
    Guid Id, string Title, Phase Phase, int SortOrder,
    bool IsDone, DateTime? CompletedAt, string? Notes,
    int? MinDaysBefore, int? MaxDaysBefore);

// ---- Release task mutations (M2 checklist engine; timeframe fields added in M8) ----
public record AddTaskInput(string Title, Phase Phase, int? MinDaysBefore = null, int? MaxDaysBefore = null);
public record UpdateTaskInput(string Title, Phase Phase, string? Notes, int? MinDaysBefore = null, int? MaxDaysBefore = null);
public record ReorderTasksInput(Phase Phase, List<Guid> OrderedTaskIds);

// ---- Tracks (v2.0: a Release↔Song join) ----
// Used by both the create-form Tracks section and the add-track endpoint. Exactly one of
// SongId (existing catalog song) / Title (new song). Isrc/Artists apply only to a new song.
public record TrackInput(Guid? SongId, string? Title, string? Isrc, List<SongArtistInput>? Artists);
// Projected from the linked Song. Title/Isrc/Artists live on the song, not the join.
// IsSongArchived (M15) badges rows whose song has been archived (only visible on an archived release).
public record TrackDto(Guid SongId, int TrackNumber, string Title, string? Isrc, bool IsFocusTrack, bool IsSongArchived, List<SongArtistDto> Artists);
public record ReorderTracksInput(List<Guid> OrderedSongIds);

// ---- Templates (M3 template management) ----
public record TemplateDto(Guid Id, ReleaseType Type, List<TemplatePhaseGroupDto> Phases);
public record TemplatePhaseGroupDto(Phase Phase, List<TemplateTaskDto> Tasks);
public record TemplateTaskDto(Guid Id, string Title, Phase Phase, int SortOrder, int? MinDaysBefore, int? MaxDaysBefore);

public record AddTemplateTaskInput(string Title, Phase Phase, int? MinDaysBefore = null, int? MaxDaysBefore = null);
public record UpdateTemplateTaskInput(string Title, Phase Phase, int? MinDaysBefore = null, int? MaxDaysBefore = null);
public record ReorderTemplateTasksInput(Phase Phase, List<Guid> OrderedTaskIds);

// ---- Validation envelope ----
public record ValidationErrorResponse(string[] Errors);
public record CreatedWithWarnings<T>(T Data, string[] Warnings);
