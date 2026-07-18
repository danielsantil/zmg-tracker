namespace Zmg.Domain;

/// <summary>
/// The pure rule the whole read side already assumes but no write path enforced (M25 defect 1):
/// an archived release is terminal and read-only. <see cref="ReleaseWarnings"/> suppresses advisories
/// on it, <see cref="PendingActions"/> emits none for it, and songs mirror the same lifecycle — but
/// field/task/track edits slipped through. Every mutating service now gates on this and returns a
/// 409 (matching the song lifecycle's "read-only" conflict). No EF here, so it's unit-tested alone.
/// </summary>
public static class ReleaseMutability
{
    /// <summary>Conflict message for any write against an archived release.</summary>
    public const string ArchivedReadOnlyMessage = "Archived releases are read-only.";

    /// <summary>Whether a release accepts edits. False once archived (terminal, non-restorable).</summary>
    public static bool CanEdit(bool isArchived) => !isArchived;
}
