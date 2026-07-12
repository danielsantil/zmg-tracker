namespace Zmg.Domain;

/// <summary>
/// Release-identifier (UPC/ISRC) state, derived from the checklist (v1.1). A blank id is expected
/// until a release is distributed to DSPs, so the soft warning only fires once the "Distribute to DSPs"
/// task is done. Pure and reused by the release list flag, the detail header, and the M10 pending engine.
/// </summary>
public static class IdentifierState
{
    /// <summary>True once the release's "Distribute to DSPs" task is checked.</summary>
    public static bool IsDistributed(IEnumerable<ReleaseTask> tasks) =>
        tasks.Any(t => t.IsDone && t.Title == SeedData.DistributeToDspsTitle);

    /// <summary>
    /// Whether a soft identifier warning should show: distributed, and UPC or ISRC still blank.
    /// Advisory only — never blocks a save.
    /// </summary>
    public static bool NeedsWarning(bool distributed, string? upc, string? isrc) =>
        distributed && (string.IsNullOrWhiteSpace(upc) || string.IsNullOrWhiteSpace(isrc));

    /// <summary>
    /// A human label for which identifiers are missing (e.g. "Missing UPC, ISRC"), or null when both
    /// are present. Independent of distribution state; callers gate on <see cref="NeedsWarning"/>.
    /// </summary>
    public static string? MissingLabel(string? upc, string? isrc)
    {
        var missing = new List<string>(2);
        if (string.IsNullOrWhiteSpace(upc)) missing.Add("UPC");
        if (string.IsNullOrWhiteSpace(isrc)) missing.Add("ISRC");
        return missing.Count == 0 ? null : "Missing " + string.Join(", ", missing);
    }
}
