namespace Zmg.Api.Services;

/// <summary>
/// Authentication settings, bound from the <c>Auth</c> and <c>Authentication:Google</c> sections
/// (v2.10/M55). Dev supplies the Google credentials through <c>dotnet user-secrets</c>, prod through
/// ACA secrets — mirroring how R2 and the connection string are handled.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Session lifetime, absolute rather than sliding. A <c>double</c> so a fractional value can prove
    /// expiry actually works during verification without waiting a week.
    /// </summary>
    public double SessionDays { get; set; } = 7;

    /// <summary>
    /// Hostnames this app may believe it is serving, used to constrain <c>X-Forwarded-Host</c>.
    ///
    /// <b>Distinct from the root <c>AllowedHosts</c> setting</b>, which is ASP.NET's host-filtering
    /// middleware. This one exists because the Cloudflare Worker rewrites <c>Host</c> when it proxies
    /// to ACA (the ingress routes on it), so the public hostname arrives in a header instead — and the
    /// ACA FQDN stays publicly reachable, meaning an unconstrained forwarded host is forgeable there.
    /// <b>Empty disables <c>X-Forwarded-Host</c> processing entirely</b> — see the note in
    /// <c>UseZmgForwardedHeaders</c>. It must not simply be passed through to
    /// <c>ForwardedHeadersOptions.AllowedHosts</c>, where an empty list means <em>allow every host</em>.
    /// </summary>
    public string[] AllowedHosts { get; set; } = [];

    /// <summary>
    /// Absolute origin to send the browser to after sign-in, or null to stay on the current origin.
    ///
    /// A dev-loop fix, empty in prod. In dev the SPA is on :5173 and the API on :5274 with Vite
    /// proxying <c>/api</c>; Google redirects to the *server's* callback, so the browser would land on
    /// :5274 and be served the API's <c>wwwroot</c> copy of the SPA — stale, or absent. Cookies ignore
    /// port (they are host-scoped), so the session itself is fine; only the landing page is wrong.
    /// </summary>
    public string? PostLoginOrigin { get; set; }

    /// <summary>Google OIDC client credentials. Read from <c>Authentication:Google:*</c>.</summary>
    public string? GoogleClientId { get; set; }
    public string? GoogleClientSecret { get; set; }

    /// <summary>
    /// The env-var names of every required auth setting that is missing, in the form callers set them.
    /// Feeds the startup fail-fast (M35) so a deploy without credentials dies at boot naming them,
    /// rather than booting fine and failing on the first person who tries to sign in.
    /// </summary>
    public IReadOnlyList<string> MissingKeys()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(GoogleClientId)) missing.Add("Authentication__Google__ClientId");
        if (string.IsNullOrWhiteSpace(GoogleClientSecret)) missing.Add("Authentication__Google__ClientSecret");
        return missing;
    }
}
