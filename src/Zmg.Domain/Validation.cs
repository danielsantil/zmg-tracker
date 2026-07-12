namespace Zmg.Domain;

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

    /// <summary>Hard rule: an artist with releases can't be deleted.</summary>
    public static ValidationResult ValidateArtistDelete(int releaseCount)
    {
        var result = new ValidationResult();
        if (releaseCount > 0)
        {
            result.Error("Can't delete an artist that has releases.");
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

    /// <summary>A track title must be non-empty (albums only in practice).</summary>
    public static ValidationResult ValidateTrackTitle(string? title)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(title))
            result.Error("Track title is required.");
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
