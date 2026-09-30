using Travelogic.Suppliers.Domain.Common;

namespace Travelogic.Suppliers.Domain.Suppliers.Events;

// Written to the outbox and published to other services.

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
