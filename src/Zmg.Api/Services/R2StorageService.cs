using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;

namespace Zmg.Api.Services;

/// <summary>
/// <see cref="IStorageService"/> over Cloudflare R2 — S3-compatible, so the AWS SDK talks to it with
/// a custom <c>ServiceURL</c>, path-style addressing and the pseudo-region <c>auto</c>. R2 is required
/// at startup (M35), so the client is built eagerly in the constructor — the settings are always present
/// by the time DI resolves this. Building it doesn't dial anything; the first request opens the socket.
/// </summary>
public sealed class R2StorageService : IStorageService, IDisposable
{
    private readonly R2Options _options;
    private readonly IAmazonS3 _client;

    public R2StorageService(IOptions<R2Options> options)
    {
        _options = options.Value;
        _client = new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
            });
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<string> UploadCoverAsync(byte[] content, string contentType, CancellationToken ct = default)
    {
        var key = CoverImage.KeyFor(Guid.NewGuid(), contentType);

        using var stream = new MemoryStream(content, writable: false);
        await _client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                DisablePayloadSigning = true,
            },
            ct);

        return $"{_options.PublicBaseUrl!.TrimEnd('/')}/{key}";
    }

    public void Dispose() => _client.Dispose();
}
