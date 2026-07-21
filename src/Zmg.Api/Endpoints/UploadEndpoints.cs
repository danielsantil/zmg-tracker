using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;

namespace Zmg.Api.Endpoints;

public static class UploadEndpoints
{
    public static void MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/uploads").WithTags("Uploads");

        // Both paths store the image in R2 and answer with its public URL; the caller then saves that
        // URL as the release's CoverUrl like any other string (no schema change, M31).

        // DisableAntiforgery: minimal APIs require the token for IFormFile binding, and this API has no
        // antiforgery middleware (nor cookies to protect — it's a same-origin SPA over a stateless API).
        group.MapPost("/cover", async (IFormFile file, ICoverUploadService covers, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            return (await covers.UploadFileAsync(stream, file.ContentType, file.Length, ct)).ToOk();
        }).DisableAntiforgery();

        group.MapPost("/cover-from-url", async (CoverUrlInput input, ICoverUploadService covers, CancellationToken ct) =>
            (await covers.UploadFromUrlAsync(input.Url, ct)).ToOk());
    }
}
