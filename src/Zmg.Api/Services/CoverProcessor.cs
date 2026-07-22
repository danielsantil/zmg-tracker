using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Zmg.Domain;

namespace Zmg.Api.Services;

/// <summary>
/// Turns an accepted upload into the one shape we store: a WebP bounded to
/// <see cref="CoverImage.MaxStoredEdge"/> (M33). Lives in the API layer rather than Domain because it
/// needs ImageSharp, and Domain stays dependency-free — the *numbers* it works to are in
/// <see cref="CoverImage"/>.
/// </summary>
public static class CoverProcessor
{
    /// <summary>
    /// Decodes, orients, downscales and re-encodes. Returns null when the bytes don't decode, which
    /// the caller turns into a 400 — a file can pass the magic-number sniff and still be truncated
    /// or corrupt further in.
    /// </summary>
    public static byte[]? Normalize(byte[] content)
    {
        try
        {
            using var image = Image.Load(content);

            // AutoOrient must run *before* the profiles are cleared: the rotation lives in the EXIF
            // orientation tag, so stripping first would store portrait photos on their side.
            image.Mutate(x => x.AutoOrient());

            if (image.Width > CoverImage.MaxStoredEdge || image.Height > CoverImage.MaxStoredEdge)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(CoverImage.MaxStoredEdge, CoverImage.MaxStoredEdge),
                    // Max = fit inside the box, keep the aspect ratio, and never upscale a small source.
                    Mode = ResizeMode.Max,
                }));
            }

            // Drop everything the camera attached — phone photos carry GPS coordinates, and none of
            // it survives usefully into a 96px tile anyway.
            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            using var output = new MemoryStream();
            // FileFormat is explicit on purpose: left to the default, ImageSharp can pick *lossless*
            // WebP, which on a photograph is larger than the JPEG that came in — the opposite of the
            // point. Quality only means anything in the lossy path.
            image.Save(output, new WebpEncoder
            {
                FileFormat = WebpFileFormatType.Lossy,
                Quality = CoverImage.StoredQuality,
            });
            return output.ToArray();
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            return null;
        }
    }
}
