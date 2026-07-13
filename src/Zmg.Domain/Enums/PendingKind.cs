namespace Zmg.Domain.Enums;

/// <summary>Why a release is surfaced as needing attention (v1.1 M10).</summary>
public enum PendingKind
{
    /// <summary>An incomplete task whose timeframe window has opened and the release hasn't shipped yet.</summary>
    TaskDue,
    /// <summary>A distributed release still missing its UPC and/or ISRC.</summary>
    MissingIdentifier,
}