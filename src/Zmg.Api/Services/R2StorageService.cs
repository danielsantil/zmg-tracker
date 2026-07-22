using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;

namespace Zmg.Api.Services;

/// <summary>
/// <see cref="IStorageService"/> over Cloudflare R2 — S3-compatible, so the AWS SDK talks to it with
/// a custom <c>ServiceURL</c>, path-style addressing and the pseudo-region <c>auto</c>. The client is
/// built lazily: an unconfigured environment (tests, a dev box without secrets) must still boot, and
/// only an actual upload attempt should complain.
/// </summary>
public sealed class R2StorageService : IStorageService, IDisposable
{
    private readonly R2Options _options;
    private readonly Lazy<IAmazonS3> _client;

    public R2StorageService(IOptions<R2Options> options)
    {
        _options = options.Value;
        _client = new Lazy<IAmazonS3>(CreateClient);
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<string> UploadCoverAsync(byte[] content, string contentType, CancellationToken ct = default)
    {
        var key = CoverImage.KeyFor(Guid.NewGuid(), contentType);

        using var stream = new MemoryStream(content, writable: false);
        await _client.Value.PutObjectAsync(
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

    private IAmazonS3 CreateClient()
    {
        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException("R2 storage is not configured. Set the R2:* settings.");
        }

        return new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
            });
    }

    public void Dispose()
    {
        if (_client.IsValueCreated) _client.Value.Dispose();
    }
}
