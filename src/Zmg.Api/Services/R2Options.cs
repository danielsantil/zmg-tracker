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

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountId)
        && !string.IsNullOrWhiteSpace(AccessKeyId)
        && !string.IsNullOrWhiteSpace(SecretAccessKey)
        && !string.IsNullOrWhiteSpace(Bucket)
        && !string.IsNullOrWhiteSpace(PublicBaseUrl);

    public string ServiceUrl => $"https://{AccountId}.r2.cloudflarestorage.com";
}
