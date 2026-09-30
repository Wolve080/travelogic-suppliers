namespace Travelogic.Suppliers.Domain.Common;

/// <summary>Something meaningful that happened inside an aggregate.</summary>
public interface IDomainEvent
{
    /// <summary>The aggregate the event belongs to. Used as the message key when published.</summary>
    Guid AggregateId { get; }
}
