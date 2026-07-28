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
    /// The request needs a session and has none — expired, revoked, or never signed in. The SPA's
    /// signal to show the login gate, which is why <c>/api/*</c> answers 401 with this rather than
    /// redirecting: an XHR that follows a 302 and parses HTML as JSON fails confusingly.
    /// </summary>
    public const string RequiredCode = "error.auth.required";

    /// <summary>
    /// Authenticated with the provider, but not permitted here — either never listed or disabled.
    /// <b>One code for both</b>, deliberately: distinguishing them would tell an outsider whether an
    /// address is known to us, which is a membership oracle for free.
    /// </summary>
    public const string NotAllowedCode = "error.auth.notAllowed";

    /// <summary>
    /// The provider returned an address it has not verified. Rare with Google, but accepting it would
    /// let anyone who can set an unverified address on some account impersonate a listed one.
    /// </summary>
    public const string EmailUnverifiedCode = "error.auth.emailUnverified";

    /// <summary>
    /// Whether a looked-up user may use the app. A null user means the address was never on the list;
    /// a non-null one with <see cref="AllowedUser.DisabledAt"/> set means access was revoked. Both are
    /// denials, and callers must not distinguish them to the browser — see the note on enumeration in
    /// the login screen's design.
    /// </summary>
    public static bool IsAllowed(AllowedUser? user) => user is { DisabledAt: null };
}
