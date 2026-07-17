namespace Zmg.Api.Tests;

/// <summary>
/// Relative test dates (M25 defect 4). A hardcoded future literal was a time bomb: the API auto-checks
/// "Distribute to DSPs" on any past-dated release, so once that literal slipped into the past every test
/// that assumed the release was still upcoming began to fail.
/// Anchoring to <see cref="Today"/> keeps "upcoming"/"past" true forever. Mirrors the four files that
/// already computed dates from <c>DateTime.UtcNow</c> (PendingApiTests, ReleaseArchiveApiTests, …).
/// </summary>
internal static class TestDates
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Comfortably in the future — never auto-distributed, no pending window open yet.</summary>
    public static DateOnly Upcoming => Today.AddMonths(2);

    /// <summary>Comfortably in the past — a backfilled release.</summary>
    public static DateOnly Past => Today.AddMonths(-2);
}
