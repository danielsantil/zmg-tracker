namespace Zmg.Api.Services;

/// <summary>
/// Cloudflare R2 settings, bound from the <c>R2</c> configuration section. Dev supplies them through
/// <c>dotnet user-secrets</c>, prod through ACA secrets (<c>R2__AccessKeyId</c> …) — the write
/// credentials never leave the server, so the SPA only ever sees the public URL an upload returns.
/// </summary>
public sealed class R2Options
{
    public const string SectionName = "R2";

    public string? AccountId { get; set; }
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string? Bucket { get; set; }

    /// <summary>Public read origin for the bucket (the r2.dev URL until a custom domain lands).</summary>
    public string? PublicBaseUrl { get; set; }

    public bool IsConfigured => MissingKeys().Count == 0;

    /// <summary>
    /// The env-var names of every required R2 setting that is missing or blank, in the form callers set
    /// them (<c>R2__AccountId</c> …). Empty when fully configured. Drives both <see cref="IsConfigured"/>
    /// and the startup fail-fast, so the two never drift.
    /// </summary>
    public IReadOnlyList<string> MissingKeys()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(AccountId)) missing.Add("R2__AccountId");
        if (string.IsNullOrWhiteSpace(AccessKeyId)) missing.Add("R2__AccessKeyId");
        if (string.IsNullOrWhiteSpace(SecretAccessKey)) missing.Add("R2__SecretAccessKey");
        if (string.IsNullOrWhiteSpace(Bucket)) missing.Add("R2__Bucket");
        if (string.IsNullOrWhiteSpace(PublicBaseUrl)) missing.Add("R2__PublicBaseUrl");
        return missing;
    }

    public string ServiceUrl => $"https://{AccountId}.r2.cloudflarestorage.com";
}
