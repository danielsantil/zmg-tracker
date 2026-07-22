using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zmg.Api.Contracts;
using Zmg.Api.Services;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;

namespace Zmg.Api.Tests;

/// <summary>
/// Cover ingest (M31). R2 is faked and every remote fetch is served by a stub handler, so these tests
/// touch neither the network nor an S3 endpoint. Remote hosts are written as public IP literals on
/// purpose — that keeps the SSRF host check off DNS and the suite hermetic.
/// </summary>
public class UploadApiTests : IClassFixture<UploadApiFactory>
{
    private readonly UploadApiFactory _factory;

    // The fixture (and so both fakes) is shared by every test in the class — reset what they recorded
    // so an assertion can't pass or fail on the previous test's leftovers.
    public UploadApiTests(UploadApiFactory factory)
    {
        _factory = factory;
        _factory.Storage.Reset();
        _factory.Remote.Reset();
    }

    // ---- POST /api/uploads/cover (multipart) ----

    [Fact]
    public async Task Upload_stores_the_file_and_returns_its_public_url()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/uploads/cover", FileContent(TestImages.Png, "image/png", "cover.png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UploadedCoverDto>();
        Assert.StartsWith(FakeStorageService.PublicBase, body!.Url);
        // Stored as WebP whatever came in (M33).
        Assert.Equal("image/webp", _factory.Storage.LastContentType);
    }

    [Fact]
    public async Task Upload_downscales_an_oversized_image_to_the_stored_bound()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/uploads/cover", FileContent(TestImages.LargePng, "image/png", "big.png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var stored = Image.Load(_factory.Storage.LastContent!);
        Assert.Equal(CoverImage.MaxStoredEdge, stored.Width);          // 1200x800 fits to 1000x667
        Assert.True(stored.Height < CoverImage.MaxStoredEdge);         // aspect ratio preserved
        Assert.True(_factory.Storage.LastContent!.Length < TestImages.LargePng.Length);
    }

    [Fact]
    public async Task Upload_does_not_upscale_an_image_smaller_than_the_bound()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/uploads/cover", FileContent(TestImages.SmallPng, "image/png", "small.png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var stored = Image.Load(_factory.Storage.LastContent!);
        Assert.Equal(64, stored.Width);
        Assert.Equal(48, stored.Height);
    }

    [Fact]
    public async Task Upload_rejects_a_file_with_a_valid_header_but_a_corrupt_body()
    {
        var client = _factory.CreateClient();
        // Passes the magic-number sniff, then fails to decode — a 400, never an unhandled 500.
        var truncated = TestImages.Png[..64];

        var response = await client.PostAsync("/api/uploads/cover", FileContent(truncated, "image/png", "truncated.png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(_factory.Storage.LastContentType);
    }

    [Fact]
    public async Task Upload_rejects_a_non_image_content_type()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/uploads/cover", FileContent(TestImages.Png, "application/pdf", "cover.pdf"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(_factory.Storage.LastContentType);
    }

    [Fact]
    public async Task Upload_rejects_a_file_whose_bytes_are_not_really_an_image()
    {
        var client = _factory.CreateClient();
        var script = "<svg onload=alert(1)>"u8.ToArray();

        // Declared as PNG; the magic-number sniff is what catches it.
        var response = await client.PostAsync("/api/uploads/cover", FileContent(script, "image/png", "cover.png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(_factory.Storage.LastContentType);
    }

    [Fact]
    public async Task Upload_rejects_a_file_over_the_size_cap()
    {
        var client = _factory.CreateClient();
        var oversized = new byte[6 * 1024 * 1024];
        TestImages.Png.CopyTo(oversized, 0);

        var response = await client.PostAsync("/api/uploads/cover", FileContent(oversized, "image/png", "huge.png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(_factory.Storage.LastContentType);
    }

    // ---- POST /api/uploads/cover-from-url ----

    [Fact]
    public async Task Upload_from_url_fetches_the_remote_image_and_stores_it_in_r2()
    {
        var client = _factory.CreateClient();
        _factory.Remote.RespondWithImage(TestImages.Jpeg, "image/jpeg");

        var response = await PostUrl(client, "http://93.184.216.34/cover.jpg");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UploadedCoverDto>();
        // Stored in R2, not hotlinked — the returned URL is ours.
        Assert.StartsWith(FakeStorageService.PublicBase, body!.Url);
        Assert.Equal("image/webp", _factory.Storage.LastContentType);
    }

    [Theory]
    [InlineData("http://127.0.0.1/cover.png")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]  // cloud metadata
    [InlineData("http://10.1.2.3/cover.png")]
    [InlineData("http://192.168.0.4/cover.png")]
    [InlineData("http://[::1]/cover.png")]
    public async Task Upload_from_url_refuses_to_dial_internal_hosts(string url)
    {
        var client = _factory.CreateClient();
        _factory.Remote.RespondWithImage(TestImages.Png, "image/png");

        var response = await PostUrl(client, url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, _factory.Remote.Requests);  // blocked before any socket is opened
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://93.184.216.34/cover.png")]
    [InlineData("")]
    public async Task Upload_from_url_rejects_non_http_urls(string url)
    {
        var client = _factory.CreateClient();

        var response = await PostUrl(client, url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, _factory.Remote.Requests);
    }

    [Fact]
    public async Task Upload_from_url_re_checks_the_target_of_a_redirect()
    {
        var client = _factory.CreateClient();
        // A public host that bounces to the metadata endpoint — the reason redirects are followed by hand.
        _factory.Remote.RespondWithRedirect("http://169.254.169.254/latest/meta-data/");

        var response = await PostUrl(client, "http://93.184.216.34/cover.png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, _factory.Remote.Requests);  // the first hop only; the redirect target was never dialled
        Assert.Null(_factory.Storage.LastContentType);
    }

    [Fact]
    public async Task Upload_from_url_rejects_a_remote_response_that_is_not_an_image()
    {
        var client = _factory.CreateClient();
        _factory.Remote.RespondWithImage("<html>not an image</html>"u8.ToArray(), "image/png");

        var response = await PostUrl(client, "http://93.184.216.34/cover.png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(_factory.Storage.LastContentType);
    }

    [Fact]
    public async Task Upload_from_url_rejects_a_remote_image_over_the_size_cap()
    {
        var client = _factory.CreateClient();
        var oversized = new byte[6 * 1024 * 1024];
        TestImages.Png.CopyTo(oversized, 0);
        _factory.Remote.RespondWithImage(oversized, "image/png");

        var response = await PostUrl(client, "http://93.184.216.34/huge.png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(_factory.Storage.LastContentType);
    }

    [Fact]
    public async Task Upload_from_url_reports_a_remote_failure_without_leaking_it()
    {
        var client = _factory.CreateClient();
        _factory.Remote.RespondWith(HttpStatusCode.NotFound);

        var response = await PostUrl(client, "http://93.184.216.34/missing.png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        Assert.Equal(Zmg.Domain.CoverImage.UnreachableUrlMessage, body!.Errors.Single());
    }

    private static Task<HttpResponseMessage> PostUrl(HttpClient client, string url) =>
        client.PostAsJsonAsync("/api/uploads/cover-from-url", new CoverUrlInput(url));

    private static MultipartFormDataContent FileContent(byte[] bytes, string contentType, string fileName)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }
}

/// <summary>Boots the API with R2 and the outbound HTTP handler faked.</summary>
public class UploadApiFactory : ZmgApiFactory
{
    public FakeStorageService Storage { get; } = new();
    public StubRemoteHost Remote { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStorageService>();
            services.AddSingleton<IStorageService>(Storage);

            // Re-registering the typed client swaps its primary handler (last registration wins), so
            // the real HttpClientHandler never opens a socket during tests.
            services.AddHttpClient<ICoverUploadService, CoverUploadService>()
                .ConfigurePrimaryHttpMessageHandler(() => Remote);
        });
    }
}

public sealed class FakeStorageService : IStorageService
{
    public const string PublicBase = "https://covers.test/";

    public string? LastContentType { get; private set; }
    public byte[]? LastContent { get; private set; }

    public bool IsConfigured => true;

    public void Reset()
    {
        LastContentType = null;
        LastContent = null;
    }

    public Task<string> UploadCoverAsync(byte[] content, string contentType, CancellationToken ct = default)
    {
        LastContentType = contentType;
        LastContent = content;
        return Task.FromResult(PublicBase + Zmg.Domain.CoverImage.KeyFor(Guid.NewGuid(), contentType));
    }
}

/// <summary>A programmable stand-in for whatever host a pasted URL points at.</summary>
public sealed class StubRemoteHost : HttpMessageHandler
{
    private Func<HttpResponseMessage> _responder = () => new HttpResponseMessage(HttpStatusCode.NotFound);

    /// <summary>How many requests actually reached the wire — 0 proves a guard fired first.</summary>
    public int Requests { get; private set; }

    public void RespondWithImage(byte[] bytes, string contentType)
    {
        Respond(() =>
        {
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });
    }

    public void RespondWithRedirect(string location) =>
        Respond(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri(location);
            return response;
        });

    public void RespondWith(HttpStatusCode status) => Respond(() => new HttpResponseMessage(status));

    /// <summary>Back to "nothing here", with the request count cleared.</summary>
    public void Reset() => Respond(() => new HttpResponseMessage(HttpStatusCode.NotFound));

    private void Respond(Func<HttpResponseMessage> responder)
    {
        _responder = responder;
        Requests = 0;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests++;
        return Task.FromResult(_responder());
    }
}

/// <summary>
/// Real, decodable images — M33 re-encodes every accepted upload, so byte headers alone no longer
/// reach the storage seam. ImageSharp comes in transitively via Zmg.Api; no test package needed.
/// </summary>
internal static class TestImages
{
    public static readonly byte[] Png = Encode(320, 320, new PngEncoder());
    public static readonly byte[] Jpeg = Encode(320, 320, new JpegEncoder());

    /// <summary>Bigger than MaxStoredEdge on both axes, so the resize has to do something.</summary>
    public static readonly byte[] LargePng = Encode(1200, 800, new PngEncoder());

    /// <summary>Smaller than MaxStoredEdge — must come back out at its original size, not upscaled.</summary>
    public static readonly byte[] SmallPng = Encode(64, 48, new PngEncoder());

    private static byte[] Encode(int width, int height, IImageEncoder encoder)
    {
        using var image = new Image<Rgba32>(width, height);
        // A flat fill compresses to almost nothing; a gradient keeps the bytes realistic.
        image.Mutate(x => x.ProcessPixelRowsAsVector4((row, point) =>
        {
            for (var i = 0; i < row.Length; i++)
            {
                row[i] = new Vector4(i / (float)row.Length, point.Y / (float)height, 0.6f, 1f);
            }
        }));

        using var buffer = new MemoryStream();
        image.Save(buffer, encoder);
        return buffer.ToArray();
    }
}
