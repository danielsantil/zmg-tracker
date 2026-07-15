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
/// but do not block (surfaced inline, dismissible).
/// </summary>
public sealed class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public bool IsValid => Errors.Count == 0;

    public ValidationResult Error(string message)
    {
        Errors.Add(message);
        return this;
    }

    public ValidationResult Warn(string message)
    {
        Warnings.Add(message);
        return this;
    }
}

/// <summary>
/// The section 6 validation rules as pure functions. Uniqueness and "has releases"
/// checks take already-loaded context so the rules stay testable without a database.
/// </summary>
public static class Validation
{
    public static ValidationResult ValidateArtist(
        string? name,
        IEnumerable<string> otherArtistNames)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(name))
        {
            result.Error("Artist name is required.");
        }
        else if (otherArtistNames.Any(n =>
                     string.Equals(n?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            result.Error($"An artist named \"{name.Trim()}\" already exists.");
        }

        return result;
    }

    /// <summary>Hard rule: an artist who is the main artist of any release or song can't be deleted.</summary>
    public static ValidationResult ValidateArtistDelete(int dependentCount)
    {
        var result = new ValidationResult();
        if (dependentCount > 0)
        {
            result.Error("Can't delete an artist that has releases or songs.");
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
            result.Error("Release title is required.");

        if (mainArtistId == Guid.Empty)
            result.Error("A main artist is required.");
        else if (!mainArtistExists)
            result.Error("The selected main artist does not exist.");

        if (releaseDate is null)
            result.Error("Release date is required.");

        // Warnings (advise, don't block)
        if (releaseDate is { } date && date < today)
            result.Warn("Release date is in the past — backfilling an old release?");

        if (!string.IsNullOrWhiteSpace(title) &&
            otherTitlesForSameArtist.Any(t =>
                string.Equals(t?.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            result.Warn($"This artist already has a release titled \"{title.Trim()}\".");
        }

        return result;
    }

    public static ValidationResult ValidateTaskTitle(string? title)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(title))
            result.Error("Task title is required.");
        return result;
    }

    /// <summary>
    /// Song create/rename rules (v2.0). Title required; a main artist is required. The optional
    /// existing-title set (same main artist) drives the non-blocking duplicate-title warning —
    /// pass an empty set to skip it (e.g. editing a song whose title didn't change).
    /// </summary>
    public static ValidationResult ValidateSong(
        string? title,
        Guid mainArtistId,
        bool mainArtistExists,
        IEnumerable<string> otherTitlesForSameArtist)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(title))
            result.Error("Song title is required.");

        if (mainArtistId == Guid.Empty)
            result.Error("A main artist is required.");
        else if (!mainArtistExists)
            result.Error("The selected main artist does not exist.");

        if (!string.IsNullOrWhiteSpace(title) &&
            otherTitlesForSameArtist.Any(t =>
                string.Equals(t?.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            result.Warn("A song with this title already exists for this artist — consider picking it from the catalog.");
        }

        return result;
    }

    /// <summary>
    /// The inline Tracks section a release is created with (v2.0). Pure structural rules only —
    /// existence/archived checks stay in the service. A single must have exactly one track; an album
    /// may have zero or more. Each spec must set exactly one of existing-song-id / new-title, no song
    /// may appear twice, and new titles must be non-blank.
    /// </summary>
    public static ValidationResult ValidateReleaseTracks(ReleaseType type, IReadOnlyList<TrackSpec> tracks)
    {
        var result = new ValidationResult();

        if (type == ReleaseType.Single && tracks.Count != 1)
            result.Error("A single must have exactly one track.");

        foreach (var spec in tracks)
        {
            var hasId = spec.ExistingSongId is { } id && id != Guid.Empty;
            var hasTitle = !string.IsNullOrWhiteSpace(spec.NewTitle);
            if (hasId == hasTitle) // both or neither
                result.Error("Each track must be either an existing song or a new title, not both or neither.");
        }

        var duplicateIds = tracks
            .Where(t => t.ExistingSongId is { } id && id != Guid.Empty)
            .GroupBy(t => t.ExistingSongId!.Value)
            .Any(g => g.Count() > 1);
        if (duplicateIds)
            result.Error("The same song can't appear twice on a release.");

        return result;
    }

    /// <summary>A template must always keep at least one task.</summary>
    public static ValidationResult ValidateTemplateTaskDelete(int remainingTaskCount)
    {
        var result = new ValidationResult();
        if (remainingTaskCount < 1)
            result.Error("A template must keep at least one task.");
        return result;
    }
}
