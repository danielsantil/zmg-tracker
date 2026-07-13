using Zmg.Api.Contracts;
using Zmg.Api.Extensions;
using Zmg.Api.Services.Interfaces;

namespace Zmg.Api.Endpoints;

public static class ArtistEndpoints
{
    public static void MapArtistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/artists").WithTags("Artists");

        group.MapGet("", async (IArtistService artists) =>
            Results.Ok(await artists.ListAsync()));

        group.MapPost("", async (ArtistInput input, IArtistService artists) =>
            (await artists.CreateAsync(input)).ToCreated(a => $"/api/artists/{a.Id}"));

        group.MapPut("/{id:guid}", async (Guid id, ArtistInput input, IArtistService artists) =>
            (await artists.UpdateAsync(id, input)).ToOk());

        group.MapDelete("/{id:guid}", async (Guid id, IArtistService artists) =>
            (await artists.DeleteAsync(id)).ToNoContent());
    }
}
