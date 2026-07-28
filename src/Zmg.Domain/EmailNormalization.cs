namespace Zmg.Domain;

/// <summary>
/// One spelling for an email address, everywhere (v2.10/M54).
///
/// Addresses arrive from three directions — a provider's token, a hand-written <c>INSERT</c>, and the
/// seed — and they must agree or the whitelist silently fails open in one direction and closed in the
/// other. Every write and every lookup passes through here, so <c>AllowedUser.Email</c> is stored
/// normalized and compared with plain ordinal <c>==</c>.
///
/// Ordinal comparison is the point: it keeps the query provider-agnostic (v2.5's rule), so the SQLite
/// tests stay representative of Postgres rather than depending on either one's collation.
/// </summary>
public static class EmailNormalization
{
    /// <summary>
    /// Trims and lowercases. Null/blank normalizes to <see cref="string.Empty"/> rather than throwing,
    /// so callers can normalize-then-validate instead of ordering the two.
    /// </summary>
    /// <remarks>
    /// The local part of an address is technically case-sensitive per RFC 5321, but no mail provider
    /// in practice treats it that way — Google certainly does not — and a whitelist that distinguishes
    /// <c>Daniel@</c> from <c>daniel@</c> is a lockout waiting to happen. Lowercasing the whole address
    /// is the correct trade here.
    ///
    /// <c>ToLowerInvariant</c> is safe under <c>InvariantGlobalization=true</c> (M41): invariant mode
    /// still performs simple case mapping and does not throw. It is also why this must never become
    /// <c>ToLower(CultureInfo.CurrentCulture)</c> — that would reintroduce the Turkish dotless-ı class
    /// of bug on a value that gates access.
    /// </remarks>
    public static string Normalize(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
}
