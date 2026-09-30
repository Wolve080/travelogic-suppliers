using Travelogic.Suppliers.Domain.Common;
using Travelogic.Suppliers.Domain.Suppliers.Events;

namespace Travelogic.Suppliers.Domain.Suppliers;

/// <summary>
/// A business that provides tourism services, e.g. a hotel offering overnight accommodation or a
/// safari operator offering a half day game drive. The supplier is the aggregate root: its services
/// can only be changed through it, which lets it enforce rules such as unique service names.
/// </summary>
public sealed class Supplier : AggregateRoot, IAuditable
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int MaxServices = 200;

    private readonly List<SupplierService> _services = [];

    private Supplier(Guid id, string name, SupplierType type, string? description, ContactDetails contact, Address address)
        : base(id)
    {
        Name = name;
        Type = type;
        Description = description;
        Contact = contact;
        Address = address;
        IsActive = true;
    }

    // Required by EF Core.
    private Supplier() { }

    public string Name { get; private set; } = null!;

    public SupplierType Type { get; private set; }

    public string? Description { get; private set; }

    public ContactDetails Contact { get; private set; } = null!;

    public Address Address { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<SupplierService> Services => _services.AsReadOnly();

    public static Supplier Create(string name, SupplierType type, string? description, ContactDetails contact, Address address)
    {
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentNullException.ThrowIfNull(address);

        // Version 7 GUIDs are time ordered, so inserts append to the clustered index instead of fragmenting it.
        var supplier = new Supplier(
            Guid.CreateVersion7(),
            Guard.Required(name, "Supplier name", NameMaxLength),
            Guard.Defined(type, "Supplier type"),
            Guard.Optional(description, "Description", DescriptionMaxLength),
            contact,
            address);

        supplier.Raise(new SupplierCreated(supplier.Id, supplier.Name, supplier.Type));
        return supplier;
    }

    public void UpdateDetails(string name, SupplierType type, string? description, ContactDetails contact, Address address, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentNullException.ThrowIfNull(address);

        Name = Guard.Required(name, "Supplier name", NameMaxLength);
        Type = Guard.Defined(type, "Supplier type");
        Description = Guard.Optional(description, "Description", DescriptionMaxLength);
        Contact = contact;
        Address = address;
        IsActive = isActive;

        Raise(new SupplierUpdated(Id, Name, Type, IsActive));
    }

    public SupplierService AddService(
        string name,
        ServiceCategory category,
        string? description,
        Money price,
        PricingUnit pricingUnit,
        int? durationMinutes,
        int? capacity)
    {
        if (_services.Count >= MaxServices)
        {
            throw new DomainException($"A supplier cannot have more than {MaxServices} services.");
        }

        EnsureServiceNameIsUnique(name, excludeServiceId: null);

        var service = new SupplierService(Guid.CreateVersion7(), Id, name, category, description, price, pricingUnit, durationMinutes, capacity);
        _services.Add(service);

        Raise(new SupplierServiceAdded(Id, service.Id, service.Name, service.Category, service.Price.Amount, service.Price.Currency));
        return service;
    }

    public SupplierService UpdateService(
        Guid serviceId,
        string name,
        ServiceCategory category,
        string? description,
        Money price,
        PricingUnit pricingUnit,
        int? durationMinutes,
        int? capacity)
    {
        var service = FindService(serviceId) ?? throw new DomainException($"Service {serviceId} does not belong to this supplier.");

        EnsureServiceNameIsUnique(name, excludeServiceId: serviceId);
        service.Update(name, category, description, price, pricingUnit, durationMinutes, capacity);

        Raise(new SupplierServiceUpdated(Id, service.Id, service.Name, service.Category, service.Price.Amount, service.Price.Currency));
        return service;
    }

    public void RemoveService(Guid serviceId)
    {
        var service = FindService(serviceId) ?? throw new DomainException($"Service {serviceId} does not belong to this supplier.");

        _services.Remove(service);
        Raise(new SupplierServiceRemoved(Id, serviceId));
    }

    public SupplierService? FindService(Guid serviceId) => _services.Find(s => s.Id == serviceId);

    /// <summary>Records that the supplier is being removed so downstream services can react.</summary>
    public void MarkDeleted() => Raise(new SupplierDeleted(Id, Name));

    private void EnsureServiceNameIsUnique(string? name, Guid? excludeServiceId)
    {
        var trimmed = name?.Trim();
        if (_services.Any(s => s.Id != excludeServiceId && string.Equals(s.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException($"This supplier already has a service called \"{trimmed}\".");
        }
    }
}
