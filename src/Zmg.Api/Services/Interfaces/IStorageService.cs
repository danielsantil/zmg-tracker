namespace Zmg.Api.Services.Interfaces;

/// <summary>
/// Object storage for cover images. One method on purpose — the upload pipeline (validation, remote
/// fetch, SSRF guards) lives in <see cref="ICoverUploadService"/>, so this stays a thin seam that
/// tests can swap for a fake without an S3 endpoint.
/// </summary>
public interface IStorageService
{
    /// <summary>False when the R2 settings are missing, so callers can fail with a clear message.</summary>
    bool IsConfigured { get; }

    /// <summary>Stores the bytes under a fresh key and returns the public URL to render.</summary>
    Task<string> UploadCoverAsync(byte[] content, string contentType, CancellationToken ct = default);
}
