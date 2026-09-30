namespace Travelogic.Suppliers.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}

/// <summary>
/// An expected failure. Expected failures are returned rather than thrown, so the API layer can map
/// them to the right HTTP status without the application layer knowing about HTTP.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Field level messages, keyed by property path (e.g. <c>services[0].price</c>).</summary>
    public IReadOnlyDictionary<string, string[]> Details { get; init; } = new Dictionary<string, string[]>();

    public static Error Validation(string message, IReadOnlyDictionary<string, string[]>? details = null) =>
        new("validation_failed", message, ErrorType.Validation) { Details = details ?? new Dictionary<string, string[]>() };

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}

public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static implicit operator Result(Error error) => new(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null) => _value = value;

    private Result(Error error) : base(error) { }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
