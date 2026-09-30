namespace Travelogic.Suppliers.Domain.Common;

/// <summary>Entities whose timestamps are maintained by the persistence layer.</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; }

    DateTimeOffset? UpdatedAtUtc { get; }
}
