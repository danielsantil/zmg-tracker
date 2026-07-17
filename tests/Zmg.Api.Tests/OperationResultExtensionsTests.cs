using Microsoft.AspNetCore.Http;
using Zmg.Api.Extensions;
using Zmg.Api.Services;

namespace Zmg.Api.Tests;

/// <summary>
/// Unit tests for the OperationResult → HTTP mapping (M25 task 11). This one function backs ~25
/// status-code assertions scattered across the integration suite; testing it directly lets those
/// duplicated "..._is_rejected" host-boot tests go.
/// </summary>
public class OperationResultExtensionsTests
{
    private static int StatusOf(IResult result) => ((IStatusCodeHttpResult)result).StatusCode!.Value;

    [Fact]
    public void ToOk_maps_success_to_200()
    {
        Assert.Equal(200, StatusOf(OperationResult<int>.Success(1).ToOk()));
    }

    [Fact]
    public void ToCreated_maps_success_to_201()
    {
        Assert.Equal(201, StatusOf(OperationResult<int>.Success(1).ToCreated(_ => "/api/things/1")));
    }

    [Fact]
    public void ToNoContent_maps_success_to_204()
    {
        Assert.Equal(204, StatusOf(OperationResult.Success().ToNoContent()));
    }

    [Fact]
    public void ToOkWithWarnings_maps_success_to_200()
    {
        Assert.Equal(200, StatusOf(OperationResult<int>.Success(1, new[] { "heads up" }).ToOkWithWarnings()));
    }

    [Theory]
    [InlineData(ResultStatus.NotFound, 404)]
    [InlineData(ResultStatus.ValidationFailed, 400)]
    [InlineData(ResultStatus.Conflict, 409)]
    [InlineData(ResultStatus.Problem, 500)]
    public void Failures_map_to_their_status_code(ResultStatus status, int expected)
    {
        // Arrange
        OperationResult<int> result = status switch
        {
            ResultStatus.NotFound => OperationResult<int>.NotFound(),
            ResultStatus.ValidationFailed => OperationResult<int>.Invalid(new[] { "bad" }),
            ResultStatus.Conflict => OperationResult<int>.Conflict(new[] { "conflict" }),
            _ => OperationResult<int>.Problem("boom"),
        };

        // Act — the failure branch is shared by every To* method; ToOk exercises it.
        var mapped = result.ToOk();

        // Assert
        Assert.Equal(expected, StatusOf(mapped));
    }
}
