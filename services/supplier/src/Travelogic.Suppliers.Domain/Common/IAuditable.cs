namespace Travelogic.Suppliers.Domain.Common;

// Set by the DbContext on save.
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; }

    DateTimeOffset? UpdatedAtUtc { get; }
}
