namespace Travelogic.Suppliers.Application.Common;

// Persistence failures the application knows how to handle. Infrastructure translates provider
// specific exceptions (EF Core, SqlException) into these so the application stays storage agnostic.

/// <summary>The row was changed by someone else since it was read (optimistic concurrency).</summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException() { }

    public ConcurrencyConflictException(string message) : base(message) { }

    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>A unique index rejected the write, typically because of a race between two requests.</summary>
public sealed class UniqueConstraintException : Exception
{
    public UniqueConstraintException() { }

    public UniqueConstraintException(string message) : base(message) { }

    public UniqueConstraintException(string message, Exception innerException) : base(message, innerException) { }
}
