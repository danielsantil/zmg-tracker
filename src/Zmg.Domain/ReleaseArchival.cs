namespace Zmg.Domain;

/// <summary>
/// The pure "can this release be manually archived?" rule (M25 task 4). Archiving is terminal and
/// only accepts a release still to come, so a release may be archived iff it isn't already archived
/// and its date is today or later — the same guard <c>ReleaseService.ArchiveAsync</c> enforces.
/// Surfaced on the release DTOs so the SPA reads it from the server instead of re-deriving
/// <c>releaseDate &gt;= today</c> in three places (mirrors <c>SongListItem.CanArchive</c>).
/// </summary>
public static class ReleaseArchival
{
    public static bool CanArchive(DateOnly releaseDate, DateOnly today, bool isArchived) =>
        !isArchived && releaseDate >= today;
}
