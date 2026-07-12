using Zmg.Domain;

namespace Zmg.Api.Contracts;

// ---- Artists ----
public record ArtistDto(Guid Id, string Name, string? Notes, int ReleaseCount);
public record ArtistInput(string Name, string? Notes);

// ---- Releases ----
public record ReleaseArtistInput(Guid ArtistId, ArtistRole Role);

public record ReleaseInput(
    string Title,
    ReleaseType Type,
    DateOnly? ReleaseDate,
    Guid MainArtistId,
    string? CoverUrl,
    string? Notes,
    List<ReleaseArtistInput>? FeaturedArtists,
    string? Upc = null,
    string? Isrc = null);

public record FeaturedArtistDto(Guid ArtistId, string Name, ArtistRole Role);

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
    string? Isrc,
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
    List<FeaturedArtistDto> FeaturedArtists,
    int DoneTasks,
    int TotalTasks,
    List<PhaseGroupDto> Phases,
    List<TrackDto> Tracks,
    string? Upc,
    string? Isrc,
    bool NeedsIdentifierWarning,
    List<PendingAction> PendingActions);

public record PhaseGroupDto(Phase Phase, int Done, int Total, List<ReleaseTaskDto> Tasks);

public record ReleaseTaskDto(
    Guid Id, string Title, Phase Phase, int SortOrder,
    bool IsDone, DateTime? CompletedAt, string? Notes,
    int? MinDaysBefore, int? MaxDaysBefore);

// ---- Release task mutations (M2 checklist engine; timeframe fields added in M8) ----
public record AddTaskInput(string Title, Phase Phase, int? MinDaysBefore = null, int? MaxDaysBefore = null);
public record UpdateTaskInput(string Title, Phase Phase, string? Notes, int? MinDaysBefore = null, int? MaxDaysBefore = null);
public record ReorderTasksInput(Phase Phase, List<Guid> OrderedTaskIds);

// ---- Tracks (M4 album support) ----
public record TrackDto(Guid Id, int TrackNumber, string Title, bool IsFocusTrack);
public record AddTrackInput(string Title);
public record UpdateTrackInput(string Title, bool IsFocusTrack);
public record ReorderTracksInput(List<Guid> OrderedTrackIds);

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
