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
}

/// <summary>Result of a service operation that returns no value (e.g. delete).</summary>
public class OperationResult
{
    public ResultStatus Status { get; }
    public IReadOnlyList<string> Errors { get; }

    protected OperationResult(ResultStatus status, IReadOnlyList<string> errors)
    {
        Status = status;
        Errors = errors;
    }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static OperationResult Success() => new(ResultStatus.Success, Array.Empty<string>());
    public static OperationResult NotFound() => new(ResultStatus.NotFound, Array.Empty<string>());
    public static OperationResult Invalid(IEnumerable<string> errors) => new(ResultStatus.ValidationFailed, errors.ToArray());
    public static OperationResult Conflict(IEnumerable<string> errors) => new(ResultStatus.Conflict, errors.ToArray());
}

/// <summary>
/// Result of a service operation that returns a value on success. <see cref="Warnings"/> carry
/// non-blocking advice (mirrors the existing <c>CreatedWithWarnings</c> envelope) and stays empty
/// for operations that don't produce any.
/// </summary>
public sealed class OperationResult<T> : OperationResult
{
    public T? Value { get; }
    public IReadOnlyList<string> Warnings { get; }

    private OperationResult(ResultStatus status, T? value, IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
        : base(status, errors)
    {
        Value = value;
        Warnings = warnings;
    }

    public static OperationResult<T> Success(T value, IEnumerable<string>? warnings = null) =>
        new(ResultStatus.Success, value, Array.Empty<string>(), warnings?.ToArray() ?? Array.Empty<string>());

    public static new OperationResult<T> NotFound() =>
        new(ResultStatus.NotFound, default, Array.Empty<string>(), Array.Empty<string>());

    public static new OperationResult<T> Invalid(IEnumerable<string> errors) =>
        new(ResultStatus.ValidationFailed, default, errors.ToArray(), Array.Empty<string>());

    public static new OperationResult<T> Conflict(IEnumerable<string> errors) =>
        new(ResultStatus.Conflict, default, errors.ToArray(), Array.Empty<string>());
}
