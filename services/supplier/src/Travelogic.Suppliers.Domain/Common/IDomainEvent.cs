namespace Travelogic.Suppliers.Domain.Common;

public interface IDomainEvent
{
    Guid AggregateId { get; }
}
