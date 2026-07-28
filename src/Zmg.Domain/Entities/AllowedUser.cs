namespace Zmg.Domain.Entities;

/// <summary>
/// An email address permitted to sign in (v2.10/M54). This is the <em>only</em> whitelist: any Google
/// account may authenticate, and this table alone decides who gets in — authentication is not
/// authorization. Rows are added by hand (one <c>INSERT</c> in Neon); there is no signup, no invite
/// flow and no admin screen, by decision.
///
/// Authorization is flat — on the list means full access, with no roles and no per-screen rules. If
/// that ever changes, permissions hang off this entity rather than replacing it.
/// </summary>
public class AllowedUser
{
    public Guid Id { get; set; }

    /// <summary>
    /// Normalized through <see cref="EmailNormalization"/> and uniquely indexed. Always compare against
    /// a normalized value — a raw address from an identity provider will not match reliably.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Filled from the provider's profile on sign-in. Cosmetic; nothing keys off it.</summary>
    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Set to revoke access while keeping the row. A delete would lose the fact that the person was
    /// ever here, and cascade away their sessions with no trace; disabling is reversible and leaves
    /// one. <see cref="AccessControl.IsAllowed"/> is the single place this is interpreted — never
    /// re-derive it by null-checking here.
    /// </summary>
    public DateTime? DisabledAt { get; set; }

    /// <summary>Live sessions. Cascade-deleted with the user, unlike the reversible disable above.</summary>
    public List<AuthSession> Sessions { get; set; } = new();
}
