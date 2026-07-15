using Zmg.Domain;
using Zmg.Domain.Enums;

namespace Zmg.Api.Contracts;

// ---- Artists ----
public record ArtistDto(Guid Id, string Name, string? Notes, int ReleaseCount);
public record ArtistInput(string Name, string? Notes);

// ---- Songs / artists on songs (v2.0) ----
public record SongArtistInput(Guid ArtistId, ArtistRole Role);
public record SongArtistDto(Guid ArtistId, string Name, ArtistRole Role);

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
    bool NeedsIdentifierWarning);

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
    bool IsArchived);

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
public record TrackDto(Guid SongId, int TrackNumber, string Title, string? Isrc, bool IsFocusTrack, List<SongArtistDto> Artists);
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
