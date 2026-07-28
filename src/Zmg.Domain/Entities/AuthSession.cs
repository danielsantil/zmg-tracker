namespace Zmg.Domain.Entities;

/// <summary>
/// One signed-in browser session (v2.10/M54).
///
/// Sessions are <em>server-side rows</em> rather than a self-contained encrypted cookie, because the
/// requirement is "7 days unless invalidated" and only a row can be invalidated: deleting it ends the
/// session on the very next request. The cookie carries nothing but <see cref="Id"/>, so it is opaque
/// — a stolen cookie is worthless once the row is gone, and revoking access needs no key rotation.
///
/// Expiry is <em>absolute</em>, not sliding: a rolling window means a stolen cookie never expires as
/// long as someone keeps using it.
/// </summary>
public class AuthSession
{
    /// <summary>
    /// The ticket-store key, and the opaque value inside the session cookie. A string rather than a
    /// Guid because ASP.NET's <c>ITicketStore</c> contract deals in string keys.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    public Guid AllowedUserId { get; set; }
    public AllowedUser? User { get; set; }

    /// <summary>
    /// Denormalized from the user, deliberately. It answers "who is signed in right now" by reading
    /// one table with no join, and it keeps a session attributable even if the user row is later
    /// edited. This is the one place a session records identity.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The serialized ASP.NET authentication ticket. Opaque to the domain — it is written and read
    /// only by the ticket store, and nothing here interprets its bytes.
    /// </summary>
    public byte[] TicketData { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; }

    /// <summary>Absolute expiry — <c>Auth:SessionDays</c> from creation, never extended by use.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Last request this session was seen on. Diagnostic only; nothing gates on it.</summary>
    public DateTime? LastSeenAt { get; set; }
}
