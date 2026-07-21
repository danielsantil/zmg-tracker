using System.Net;
using System.Net.Sockets;
using Zmg.Api.Contracts;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;

namespace Zmg.Api.Services;

/// <summary>
/// Validates and stores cover images. Every acceptance rule comes from pure <see cref="CoverImage"/>;
/// this class only does the I/O around it — read the stream, dial the remote host, hand bytes to
/// <see cref="IStorageService"/>.
/// </summary>
public sealed class CoverUploadService : ICoverUploadService
{
    private readonly IStorageService _storage;
    private readonly HttpClient _http;
    private readonly ILogger<CoverUploadService> _logger;

    public CoverUploadService(IStorageService storage, HttpClient http, ILogger<CoverUploadService> logger)
    {
        _storage = storage;
        _http = http;
        _logger = logger;
    }

    public async Task<OperationResult<UploadedCoverDto>> UploadFileAsync(
        Stream content, string? declaredContentType, long? declaredLength, CancellationToken ct = default)
    {
        if (!_storage.IsConfigured) return NotConfigured();

        // The declared type is a cheap early reject; the bytes below are what actually decide.
        if (!CoverImage.IsAllowedContentType(declaredContentType))
        {
            return OperationResult<UploadedCoverDto>.Invalid(new[] { CoverImage.InvalidTypeMessage });
        }

        if (declaredLength > CoverImage.MaxBytes)
        {
            return OperationResult<UploadedCoverDto>.Invalid(new[] { CoverImage.TooLargeMessage });
        }

        var bytes = await ReadCappedAsync(content, ct);
        if (bytes is null) return OperationResult<UploadedCoverDto>.Invalid(new[] { CoverImage.TooLargeMessage });

        return await StoreAsync(bytes, ct);
    }

    public async Task<OperationResult<UploadedCoverDto>> UploadFromUrlAsync(string? url, CancellationToken ct = default)
    {
        if (!_storage.IsConfigured) return NotConfigured();

        if (!CoverImage.IsFetchableUrl(url, out var uri))
        {
            return OperationResult<UploadedCoverDto>.Invalid(new[] { CoverImage.InvalidUrlMessage });
        }

        byte[] bytes;
        try
        {
            var fetched = await FetchAsync(uri!, ct);
            if (!fetched.IsSuccess) return OperationResult<UploadedCoverDto>.Invalid(fetched.Errors);
            bytes = fetched.Value!;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            // Never surface the remote failure verbatim — it's a probe oracle, and useless to the user.
            _logger.LogInformation(ex, "Cover fetch failed for {Host}", uri!.Host);
            return OperationResult<UploadedCoverDto>.Invalid(new[] { CoverImage.UnreachableUrlMessage });
        }

        return await StoreAsync(bytes, ct);
    }

    /// <summary>
    /// Fetches the remote image with the SSRF guards applied to <em>every</em> hop: a host that
    /// resolves to a public address can still 302 to <c>169.254.169.254</c>, so redirects are followed
    /// by hand (auto-redirect is off on the handler) and each target is re-checked before it's dialled.
    /// </summary>
    private async Task<OperationResult<byte[]>> FetchAsync(Uri uri, CancellationToken ct)
    {
        for (var hop = 0; hop <= CoverImage.MaxRedirects; hop++)
        {
            if (!await IsAllowedHostAsync(uri, ct))
            {
                return OperationResult<byte[]>.Invalid(new[] { CoverImage.BlockedUrlMessage });
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (IsRedirect(response.StatusCode))
            {
                var location = response.Headers.Location;
                if (location is null) return OperationResult<byte[]>.Invalid(new[] { CoverImage.UnreachableUrlMessage });

                var next = location.IsAbsoluteUri ? location : new Uri(uri, location);
                if (!CoverImage.IsFetchableUrl(next.ToString(), out var parsed))
                {
                    return OperationResult<byte[]>.Invalid(new[] { CoverImage.BlockedUrlMessage });
                }

                uri = parsed!;
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return OperationResult<byte[]>.Invalid(new[] { CoverImage.UnreachableUrlMessage });
            }

            if (response.Content.Headers.ContentLength > CoverImage.MaxBytes)
            {
                return OperationResult<byte[]>.Invalid(new[] { CoverImage.TooLargeMessage });
            }

            // A lying Content-Length is why the read below is capped rather than trusted.
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var bytes = await ReadCappedAsync(stream, ct);
            return bytes is null
                ? OperationResult<byte[]>.Invalid(new[] { CoverImage.TooLargeMessage })
                : OperationResult<byte[]>.Success(bytes);
        }

        return OperationResult<byte[]>.Invalid(new[] { CoverImage.UnreachableUrlMessage });
    }

    private static bool IsRedirect(HttpStatusCode status) =>
        status is HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    /// <summary>Resolves the host and requires every address behind it to be publicly routable.</summary>
    private static async Task<bool> IsAllowedHostAsync(Uri uri, CancellationToken ct)
    {
        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out var literal))
        {
            return !CoverImage.IsBlockedAddress(literal);
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, ct);
            return CoverImage.AreAllAddressesAllowed(addresses);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Reads at most <see cref="CoverImage.MaxBytes"/>; null means the source went over.</summary>
    private static async Task<byte[]?> ReadCappedAsync(Stream source, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > CoverImage.MaxBytes) return null;
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    /// <summary>Stores bytes whose real type passed the magic-number sniff.</summary>
    private async Task<OperationResult<UploadedCoverDto>> StoreAsync(byte[] bytes, CancellationToken ct)
    {
        var sniffed = CoverImage.SniffContentType(bytes);
        if (sniffed is null) return OperationResult<UploadedCoverDto>.Invalid(new[] { CoverImage.InvalidTypeMessage });

        var url = await _storage.UploadCoverAsync(bytes, sniffed, ct);
        return OperationResult<UploadedCoverDto>.Success(new UploadedCoverDto(url));
    }

    private static OperationResult<UploadedCoverDto> NotConfigured() =>
        OperationResult<UploadedCoverDto>.Problem("Cover storage is not configured.");
}
