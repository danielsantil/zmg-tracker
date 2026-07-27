using Zmg.Domain.Enums;

namespace Zmg.Domain;

/// <summary>
/// One track a release is being created with: either an existing catalog song (<paramref name="ExistingSongId"/>)
/// or a brand-new song (<paramref name="NewTitle"/>). Exactly one must be set. Pure input to
/// <see cref="Validation.ValidateReleaseTracks"/>; existence/archived checks stay in the service.
/// </summary>
public readonly record struct TrackSpec(Guid? ExistingSongId, string? NewTitle);

/// <summary>
/// Outcome of a validation pass. Errors are hard failures (400/409); warnings advise
/// but do not block (surfaced inline, dismissible). Both carry <see cref="Message"/> codes,
/// never prose — the SPA renders them (M46).
/// </summary>
public sealed class ValidationResult
{
    public List<Message> Errors { get; } = new();
    public List<Message> Warnings { get; } = new();
    public bool IsValid => Errors.Count == 0;

    public ValidationResult Error(Message message)
    {
        Errors.Add(message);
        return this;
    }

    public ValidationResult Warn(Message message)
    {
        Warnings.Add(message);
        return this;
    }
}

/// <summary>
/// The section 6 validation rules as pure functions. Uniqueness and "has releases"
/// checks take already-loaded context so the rules stay testable without a database.
/// Every outcome is a <see cref="Message"/> code (M46) — see <see cref="Message"/> for why.
/// </summary>
public static class Validation
{
    /// <summary>
    /// Blocking code for a song title that clashes with another active song of the same main
    /// artist. Shared by <see cref="ValidateSong"/>, the release-create inline tracks, and the
    /// track-add endpoint so the rule reads identically everywhere (and the SPA can recognise it).
    /// </summary>
    public const string DuplicateSongTitleCode = "error.song.duplicateTitle";

    public const string ArtistNameRequiredCode = "error.artist.nameRequired";
    public const string DuplicateArtistNameCode = "error.artist.duplicateName";
    public const string ArtistHasDependentsCode = "error.artist.hasDependents";
    public const string ReleaseTitleRequiredCode = "error.release.titleRequired";
    public const string ReleaseDateRequiredCode = "error.release.dateRequired";
    public const string MainArtistRequiredCode = "error.mainArtistRequired";
    public const string MainArtistNotFoundCode = "error.mainArtistNotFound";
    public const string TaskTitleRequiredCode = "error.task.titleRequired";
    public const string SongTitleRequiredCode = "error.song.titleRequired";
    public const string SingleNeedsOneTrackCode = "error.tracks.singleNeedsOneTrack";
    public const string TrackSpecAmbiguousCode = "error.tracks.eitherSongOrTitle";
    public const string DuplicateTrackSongCode = "error.tracks.duplicateSong";
    public const string TemplateNeedsATaskCode = "error.template.lastTask";

    /// <summary>Advisory codes (non-blocking) raised by <see cref="ValidateRelease"/>.</summary>
    public const string PastReleaseDateCode = "warning.release.pastDate";
    public const string DuplicateReleaseTitleCode = "warning.release.duplicateTitle";

    public static ValidationResult ValidateArtist(
        string? name,
        IEnumerable<string> otherArtistNames)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(name))
        {
            result.Error(ArtistNameRequiredCode);
        }
        else if (otherArtistNames.Any(n =>
                     string.Equals(n?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            result.Error(Message.With(DuplicateArtistNameCode, ("name", name.Trim())));
        }

        return result;
    }

    /// <summary>Hard rule: an artist who is the main artist of any release or song can't be deleted.</summary>
    public static ValidationResult ValidateArtistDelete(int dependentCount)
    {
        var result = new ValidationResult();
        if (dependentCount > 0)
        {
            result.Error(ArtistHasDependentsCode);
        }
        return result;
    }

    /// <summary>
    /// Release create/edit rules. <paramref name="today"/> and the existing-title set are
    /// passed in so the warning rules stay pure. Pass an empty set to skip the duplicate
    /// check (e.g. on edit of the same release).
    /// </summary>
    public static ValidationResult ValidateRelease(
        string? title,
        Guid mainArtistId,
        bool mainArtistExists,
        DateOnly? releaseDate,
        DateOnly today,
        IEnumerable<string> otherTitlesForSameArtist)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(title))
            result.Error(ReleaseTitleRequiredCode);

        if (mainArtistId == Guid.Empty)
            result.Error(MainArtistRequiredCode);
        else if (!mainArtistExists)
            result.Error(MainArtistNotFoundCode);

        if (releaseDate is null)
            result.Error(ReleaseDateRequiredCode);

        // Warnings (advise, don't block)
        if (releaseDate is { } date && date < today)
            result.Warn(PastReleaseDateCode);

        if (!string.IsNullOrWhiteSpace(title) &&
            otherTitlesForSameArtist.Any(t =>
                string.Equals(t?.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            result.Warn(Message.With(DuplicateReleaseTitleCode, ("title", title.Trim())));
        }

        return result;
    }

    public static ValidationResult ValidateTaskTitle(string? title)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(title))
            result.Error(TaskTitleRequiredCode);
        return result;
    }

    /// <summary>
    /// Song create/rename rules (v2.0). Title required; a main artist is required. A title clashing
    /// with another active song of the same main artist is a hard error — song titles are unique per
    /// artist, so a duplicate must be resolved (rename, or reuse the existing song) rather than
    /// created. Pass an empty existing-title set to skip the check (e.g. editing a song whose title
    /// didn't change).
    /// </summary>
    public static ValidationResult ValidateSong(
        string? title,
        Guid mainArtistId,
        bool mainArtistExists,
        IEnumerable<string> otherTitlesForSameArtist)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(title))
            result.Error(SongTitleRequiredCode);

        if (mainArtistId == Guid.Empty)
            result.Error(MainArtistRequiredCode);
        else if (!mainArtistExists)
            result.Error(MainArtistNotFoundCode);

        if (!string.IsNullOrWhiteSpace(title) &&
            otherTitlesForSameArtist.Any(t =>
                string.Equals(t?.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            result.Error(DuplicateSongTitleCode);
        }

        return result;
    }

    /// <summary>
    /// The inline Tracks section a release is created with (v2.0). Pure structural rules plus the
    /// per-artist song-title uniqueness rule — existence/archived checks stay in the service. A single
    /// must have exactly one track; an album may have zero or more. Each spec must set exactly one of
    /// existing-song-id / new-title, no song may appear twice, and new titles must be non-blank. A new
    /// title that clashes with another new title in the same request, or with an active same-artist song
    /// in <paramref name="activeTitlesForArtist"/>, is a hard <see cref="DuplicateSongTitleCode"/>
    /// error (M25: hoisted out of the two services that duplicated it, incl. the within-request dedupe).
    /// </summary>
    public static ValidationResult ValidateReleaseTracks(
        ReleaseType type,
        IReadOnlyList<TrackSpec> tracks,
        IEnumerable<string> activeTitlesForArtist)
    {
        var result = new ValidationResult();

        if (type == ReleaseType.Single && tracks.Count != 1)
            result.Error(SingleNeedsOneTrackCode);

        foreach (var spec in tracks)
        {
            var hasId = spec.ExistingSongId is { } id && id != Guid.Empty;
            var hasTitle = !string.IsNullOrWhiteSpace(spec.NewTitle);
            if (hasId == hasTitle) // both or neither
                result.Error(TrackSpecAmbiguousCode);
        }

        var duplicateIds = tracks
            .Where(t => t.ExistingSongId is { } id && id != Guid.Empty)
            .GroupBy(t => t.ExistingSongId!.Value)
            .Any(g => g.Count() > 1);
        if (duplicateIds)
            result.Error(DuplicateTrackSongCode);

        // Per-artist title uniqueness for the new inline songs: a new title clashing with an active
        // same-artist song, or repeated within this request, must be renamed or linked as the existing
        // song instead of silently minting a duplicate. One message covers the whole tracklist.
        var seenNewTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var spec in tracks.Where(t => !string.IsNullOrWhiteSpace(t.NewTitle)))
        {
            var newTitle = spec.NewTitle!.Trim();
            var clashesActive = activeTitlesForArtist.Any(t =>
                string.Equals(t?.Trim(), newTitle, StringComparison.OrdinalIgnoreCase));
            if (clashesActive || !seenNewTitles.Add(newTitle))
            {
                result.Error(DuplicateSongTitleCode);
                break;
            }
        }

        return result;
    }

    /// <summary>A template must always keep at least one task.</summary>
    public static ValidationResult ValidateTemplateTaskDelete(int remainingTaskCount)
    {
        var result = new ValidationResult();
        if (remainingTaskCount < 1)
            result.Error(TemplateNeedsATaskCode);
        return result;
    }
}
