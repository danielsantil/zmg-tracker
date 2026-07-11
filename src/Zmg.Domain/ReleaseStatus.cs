namespace Zmg.Domain;

/// <summary>
/// Release status is derived, not stored (build-plan.md section 9):
/// Complete when every task is done, else Released once the date has passed,
/// else Upcoming.
/// </summary>
public static class ReleaseStatus
{
    public const string Upcoming = "Upcoming";
    public const string Released = "Released";
    public const string Complete = "Complete";

    public static string Derive(DateOnly releaseDate, DateOnly today, ProgressCount progress)
    {
        if (progress.Total > 0 && progress.Done == progress.Total)
            return Complete;
        return releaseDate <= today ? Released : Upcoming;
    }
}
