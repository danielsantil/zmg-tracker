using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;

namespace Zmg.Api.Endpoints;

public static class ArtistEndpoints
{
    public static void MapArtistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/artists").WithTags("Artists");

        group.MapGet("", async (IArtistService artists, CancellationToken ct) =>
            Results.Ok(await artists.ListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, IArtistService artists, CancellationToken ct) =>
            (await artists.GetAsync(id, ct)).ToOk());

        group.MapPost("", async (ArtistInput input, IArtistService artists, CancellationToken ct) =>
            (await artists.CreateAsync(input, ct)).ToCreated(a => $"/api/artists/{a.Id}"));

        group.MapPut("/{id:guid}", async (Guid id, ArtistInput input, IArtistService artists, CancellationToken ct) =>
            (await artists.UpdateAsync(id, input, ct)).ToOk());

        group.MapDelete("/{id:guid}", async (Guid id, IArtistService artists, CancellationToken ct) =>
            (await artists.DeleteAsync(id, ct)).ToNoContent());
    }
}
