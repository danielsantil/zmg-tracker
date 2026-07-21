using Zmg.Api.Contracts;

namespace Zmg.Api.Services.Interfaces;

/// <summary>
/// The two cover ingest paths. Both end in R2 — a pasted URL is fetched server-side and re-uploaded
/// rather than hotlinked, so a release cover never depends on someone else's host staying up.
/// </summary>
public interface ICoverUploadService
{
    Task<OperationResult<UploadedCoverDto>> UploadFileAsync(
        Stream content, string? declaredContentType, long? declaredLength, CancellationToken ct = default);

    Task<OperationResult<UploadedCoverDto>> UploadFromUrlAsync(string? url, CancellationToken ct = default);
}
