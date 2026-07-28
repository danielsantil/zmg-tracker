using Zmg.Domain.Entities;

namespace Zmg.Domain;

/// <summary>
/// The whole of authorization, in one expression (v2.10/M54).
///
/// Access is flat: an address is on the list and enabled, or it is not. There are no roles, no
/// per-screen rules and no resource ownership — so every deny path in the API calls this rather than
/// re-deriving "not null and not disabled", which is the shape that drifts once it exists in three
/// places.
///
/// This is deliberately <em>not</em> the same question as "did the user prove who they are". Any
/// Google account can authenticate; this decides whether that identity gets in.
/// </summary>
public static class AccessControl
{
    /// <summary>
    /// Whether a looked-up user may use the app. A null user means the address was never on the list;
    /// a non-null one with <see cref="AllowedUser.DisabledAt"/> set means access was revoked. Both are
    /// denials, and callers must not distinguish them to the browser — see the note on enumeration in
    /// the login screen's design.
    /// </summary>
    public static bool IsAllowed(AllowedUser? user) => user is { DisabledAt: null };
}
