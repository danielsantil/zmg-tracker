using Zmg.Api.Contracts;
using Zmg.Api.Services;

namespace Zmg.Api.Extensions;

/// <summary>
/// The single place that turns a transport-agnostic <see cref="OperationResult"/> into an HTTP
/// response. Keeping the failure mapping here means the validation→400 / not-found→404 /
/// conflict→409 boilerplate lives once instead of in every endpoint.
/// </summary>
public static class OperationResultExtensions
{
    public static IResult ToOk<T>(this OperationResult<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : Failure(result);

    public static IResult ToCreated<T>(this OperationResult<T> result, Func<T, string> location) =>
        result.IsSuccess ? Results.Created(location(result.Value!), result.Value) : Failure(result);

    public static IResult ToNoContent(this OperationResult result) =>
        result.IsSuccess ? Results.NoContent() : Failure(result);

    private static IResult Failure(OperationResult result) => result.Status switch
    {
        ResultStatus.NotFound => Results.NotFound(),
        ResultStatus.ValidationFailed => Results.BadRequest(new ValidationErrorResponse(result.Errors.ToArray())),
        ResultStatus.Conflict => Results.Conflict(new ValidationErrorResponse(result.Errors.ToArray())),
        _ => Results.Problem("Unexpected operation result."),
    };
}
