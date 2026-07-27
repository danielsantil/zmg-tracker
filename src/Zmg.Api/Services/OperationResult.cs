using Zmg.Domain;

namespace Zmg.Api.Services;

/// <summary>
/// Outcome kind for a service operation. Transport-agnostic on purpose: services never
/// return <see cref="IResult"/>, so they stay unit-testable without the HTTP stack. The
/// endpoint translates one of these into the right status code (see OperationResultExtensions).
/// </summary>
public enum ResultStatus
{
    Success,
    NotFound,
    ValidationFailed,
    Conflict,
    Problem,
}

/// <summary>
/// Result of a service operation that returns no value (e.g. delete). <see cref="Errors"/> carries
/// <see cref="Message"/> codes, not prose (M46) — the SPA owns every user-facing sentence.
/// <see cref="Problem"/> is the one exception: a 500's detail is developer-facing text, wrapped in a
/// code-less message so the shape stays uniform.
/// </summary>
public class OperationResult
{
    public ResultStatus Status { get; }
    public IReadOnlyList<Message> Errors { get; }

    protected OperationResult(ResultStatus status, IReadOnlyList<Message> errors)
    {
        Status = status;
        Errors = errors;
    }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static OperationResult Success() => new(ResultStatus.Success, Array.Empty<Message>());
    public static OperationResult NotFound() => new(ResultStatus.NotFound, Array.Empty<Message>());
    public static OperationResult Invalid(IEnumerable<Message> errors) => new(ResultStatus.ValidationFailed, errors.ToArray());
    public static OperationResult Conflict(IEnumerable<Message> errors) => new(ResultStatus.Conflict, errors.ToArray());

    /// <summary>An unexpected server-side condition (maps to 500 ProblemDetails), e.g. missing seed data.</summary>
    public static OperationResult Problem(string detail) => new(ResultStatus.Problem, new Message[] { new(detail) });
}

/// <summary>
/// Result of a service operation that returns a value on success. <see cref="Warnings"/> carry
/// non-blocking advice (mirrors the existing <c>CreatedWithWarnings</c> envelope) and stays empty
/// for operations that don't produce any.
/// </summary>
public sealed class OperationResult<T> : OperationResult
{
    public T? Value { get; }
    public IReadOnlyList<Message> Warnings { get; }

    private OperationResult(ResultStatus status, T? value, IReadOnlyList<Message> errors, IReadOnlyList<Message> warnings)
        : base(status, errors)
    {
        Value = value;
        Warnings = warnings;
    }

    public static OperationResult<T> Success(T value, IEnumerable<Message>? warnings = null) =>
        new(ResultStatus.Success, value, Array.Empty<Message>(), warnings?.ToArray() ?? Array.Empty<Message>());

    public static new OperationResult<T> NotFound() =>
        new(ResultStatus.NotFound, default, Array.Empty<Message>(), Array.Empty<Message>());

    public static new OperationResult<T> Invalid(IEnumerable<Message> errors) =>
        new(ResultStatus.ValidationFailed, default, errors.ToArray(), Array.Empty<Message>());

    public static new OperationResult<T> Conflict(IEnumerable<Message> errors) =>
        new(ResultStatus.Conflict, default, errors.ToArray(), Array.Empty<Message>());

    public static new OperationResult<T> Problem(string detail) =>
        new(ResultStatus.Problem, default, new Message[] { new(detail) }, Array.Empty<Message>());
}
