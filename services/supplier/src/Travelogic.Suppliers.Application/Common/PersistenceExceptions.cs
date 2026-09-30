namespace Travelogic.Suppliers.Application.Common;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException() { }

    public ConcurrencyConflictException(string message) : base(message) { }

    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class UniqueConstraintException : Exception
{
    public UniqueConstraintException() { }

    public UniqueConstraintException(string message) : base(message) { }

    public UniqueConstraintException(string message, Exception innerException) : base(message, innerException) { }
}
