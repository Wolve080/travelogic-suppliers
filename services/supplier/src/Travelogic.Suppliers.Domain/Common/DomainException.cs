namespace Travelogic.Suppliers.Domain.Common;

/// <summary>Thrown when an operation would break a business invariant.</summary>
public sealed class DomainException : Exception
{
    public DomainException() { }

    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
