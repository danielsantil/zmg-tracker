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
    List<ReleaseArtistInput>? FeaturedArtists);

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
    string Status);

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
    List<PhaseGroupDto> Phases);

public record PhaseGroupDto(Phase Phase, int Done, int Total, List<ReleaseTaskDto> Tasks);

public record ReleaseTaskDto(
    Guid Id, string Title, Phase Phase, int SortOrder,
    bool IsDone, DateTime? CompletedAt, string? Notes);

// ---- Validation envelope ----
public record ValidationErrorResponse(string[] Errors);
public record CreatedWithWarnings<T>(T Data, string[] Warnings);
