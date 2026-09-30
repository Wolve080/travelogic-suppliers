using Travelogic.Suppliers.Domain.Common;

namespace Travelogic.Suppliers.Domain.Suppliers.Events;

// These events are written to the outbox in the same transaction as the change, then published for
// other services (bookings, pricing, search) to consume. They carry just enough data for a consumer
// to act without calling back into this service.

public sealed record SupplierCreated(Guid AggregateId, string Name, SupplierType Type) : IDomainEvent;

public sealed record SupplierUpdated(Guid AggregateId, string Name, SupplierType Type, bool IsActive) : IDomainEvent;

public sealed record SupplierDeleted(Guid AggregateId, string Name) : IDomainEvent;

public sealed record SupplierServiceAdded(
    Guid AggregateId,
    Guid ServiceId,
    string Name,
    ServiceCategory Category,
    decimal Price,
    string Currency) : IDomainEvent;

public sealed record SupplierServiceUpdated(
    Guid AggregateId,
    Guid ServiceId,
    string Name,
    ServiceCategory Category,
    decimal Price,
    string Currency) : IDomainEvent;

public sealed record SupplierServiceRemoved(Guid AggregateId, Guid ServiceId) : IDomainEvent;
