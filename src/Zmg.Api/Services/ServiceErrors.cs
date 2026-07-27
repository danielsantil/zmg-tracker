namespace Zmg.Api.Services;

/// <summary>
/// The error codes minted by the service layer rather than by a pure domain rule (M46) — lifecycle
/// and existence conflicts that need the database to detect, so they can't live in
/// <c>Zmg.Domain.Validation</c>. One home because several are raised from more than one service
/// (the archived-song rule fires on both release-create and track-add; the reorder-mismatch rule
/// fires on tasks, template tasks and tracks), and a literal copied into three files is a literal
/// that drifts.
/// </summary>
/// <remarks>
/// Codes are permanent identifiers and map 1:1 onto the SPA's i18next key paths — renaming one is a
/// breaking change on both sides at once. Rules that <i>are</i> pure keep their codes next to
/// themselves: <c>Validation.*</c>, <c>ReleaseMutability.ArchivedReadOnlyCode</c>,
/// <c>CoverImage.*Code</c>, <c>ReleaseWarnings.*</c>.
/// </remarks>
public static class ServiceErrors
{
    // ---- Tracks ----
    public const string SingleIsFull = "error.tracks.singleIsFull";
    public const string SongNotFound = "error.tracks.songNotFound";
    public const string SongsNotFound = "error.tracks.songsNotFound";
    public const string ArchivedSong = "error.tracks.archivedSong";
    public const string SongAlreadyOnRelease = "error.tracks.alreadyOnRelease";
    public const string TrackReorderMismatch = "error.tracks.reorderMismatch";

    // ---- Tasks (release checklist + template) ----
    public const string TaskReorderMismatch = "error.task.reorderMismatch";

    // ---- Release lifecycle ----
    public const string ReleaseTypeImmutable = "error.release.typeImmutable";
    public const string ReleaseAlreadyArchived = "error.release.alreadyArchived";
    public const string ReleaseArchiveTooLate = "error.release.archiveTooLate";
    public const string ReleaseDeleteNotArchived = "error.release.deleteNotArchived";

    // ---- Song lifecycle ----
    public const string SongArchivedReadOnly = "error.song.archivedReadOnly";
    public const string SongMainArtistImmutable = "error.song.mainArtistImmutable";
    public const string SongAlreadyArchived = "error.song.alreadyArchived";
    public const string SongOnActiveRelease = "error.song.onActiveRelease";
    public const string SongDeleteNotArchived = "error.song.deleteNotArchived";
}
