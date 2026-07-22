using System.Net;

namespace Zmg.Domain.Tests;

public class CoverImageTests
{
    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/webp")]
    [InlineData("IMAGE/PNG")]
    [InlineData("image/png; charset=binary")]
    public void IsAllowedContentType_accepts_the_three_image_types(string contentType)
    {
        Assert.True(CoverImage.IsAllowedContentType(contentType));
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("image/svg+xml")]      // scriptable — deliberately not allowed
    [InlineData("text/html")]
    [InlineData("application/pdf")]
    [InlineData("")]
    [InlineData(null)]
    public void IsAllowedContentType_rejects_everything_else(string? contentType)
    {
        Assert.False(CoverImage.IsAllowedContentType(contentType));
    }

    [Fact]
    public void ExtensionFor_maps_each_allowed_type()
    {
        Assert.Equal(".png", CoverImage.ExtensionFor("image/png"));
        Assert.Equal(".jpg", CoverImage.ExtensionFor("image/jpeg"));
        Assert.Equal(".webp", CoverImage.ExtensionFor("image/webp"));
    }

    [Fact]
    public void SniffContentType_reads_the_magic_number_of_each_format()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
        Assert.Equal("image/png", CoverImage.SniffContentType(png));

        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        Assert.Equal("image/jpeg", CoverImage.SniffContentType(jpeg));
        
        var webp = "RIFF\0\0\0\0WEBPVP8 "u8.ToArray();
        Assert.Equal("image/webp", CoverImage.SniffContentType(webp));
    }

    [Fact]
    public void SniffContentType_returns_null_for_non_image_bytes()
    {
        Assert.Null(CoverImage.SniffContentType([0x3C, 0x73, 0x76, 0x67])); // "<svg"
        Assert.Null(CoverImage.SniffContentType([]));
        Assert.Null(CoverImage.SniffContentType([0x89, 0x50]));             // truncated PNG header
    }

    [Fact]
    public void SniffContentType_rejects_RIFF_that_is_not_WEBP()
    {
        var riffWave = "RIFF\0\0\0\0WAVE"u8.ToArray();
        Assert.Null(CoverImage.SniffContentType(riffWave));
    }

    [Theory]
    [InlineData("https://example.com/cover.png")]
    [InlineData("http://example.com/cover.png")]
    public void IsFetchableUrl_accepts_absolute_http_urls(string url)
    {
        Assert.True(CoverImage.IsFetchableUrl(url, out var uri));
        Assert.NotNull(uri);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/cover.png")]
    [InlineData("gopher://example.com/")]
    [InlineData("/covers/local.png")]  // relative — no host to check
    [InlineData("not a url")]
    [InlineData("")]
    [InlineData(null)]
    public void IsFetchableUrl_rejects_non_http_schemes_and_junk(string? url)
    {
        Assert.False(CoverImage.IsFetchableUrl(url, out var uri));
        Assert.Null(uri);
    }

    [Theory]
    [InlineData("127.0.0.1")]          // loopback
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]   // IPv4-mapped loopback
    [InlineData("10.0.0.5")]
    [InlineData("172.16.4.4")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.169.254")]    // cloud metadata endpoint
    [InlineData("100.64.0.1")]         // CGNAT
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]          // multicast
    [InlineData("fd00::1")]            // IPv6 unique local
    [InlineData("fe80::1")]            // IPv6 link local
    public void IsBlockedAddress_blocks_internal_addresses(string address)
    {
        Assert.True(CoverImage.IsBlockedAddress(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("172.32.0.1")]         // just outside 172.16.0.0/12
    [InlineData("172.15.255.255")]
    [InlineData("192.169.0.1")]        // just outside 192.168.0.0/16
    [InlineData("2606:4700:4700::1111")]
    public void IsBlockedAddress_allows_public_addresses(string address)
    {
        Assert.False(CoverImage.IsBlockedAddress(IPAddress.Parse(address)));
    }

    [Fact]
    public void AreAllAddressesAllowed_fails_when_any_address_is_internal()
    {
        // A hostname that resolves to both a public and a loopback address is the classic
        // DNS-rebinding shape — one bad answer poisons the whole set.
        var mixed = new[] { IPAddress.Parse("8.8.8.8"), IPAddress.Loopback };
        Assert.False(CoverImage.AreAllAddressesAllowed(mixed));
    }

    [Fact]
    public void AreAllAddressesAllowed_fails_when_nothing_resolved()
    {
        Assert.False(CoverImage.AreAllAddressesAllowed(Array.Empty<IPAddress>()));
    }

    [Fact]
    public void KeyFor_namespaces_covers_and_uses_the_type_extension()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Assert.Equal("covers/11111111222233334444555555555555.jpg", CoverImage.KeyFor(id, "image/jpeg"));
    }
}
