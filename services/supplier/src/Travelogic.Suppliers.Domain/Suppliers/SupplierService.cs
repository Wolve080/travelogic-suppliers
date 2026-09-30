using Travelogic.Suppliers.Domain.Common;

namespace Travelogic.Suppliers.Domain.Suppliers;

/// <summary>
/// Something a supplier sells, such as a night's accommodation or a half day tour (an activity).
/// Only reachable through its <see cref="Supplier"/>.
/// </summary>
public sealed class SupplierService : Entity, IAuditable
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    internal SupplierService(
        Guid id,
        Guid supplierId,
        string name,
        ServiceCategory category,
        string? description,
        Money price,
        PricingUnit pricingUnit,
        int? durationMinutes,
        int? capacity)
        : base(id)
    {
        SupplierId = supplierId;
        Update(name, category, description, price, pricingUnit, durationMinutes, capacity);
    }

    // Required by EF Core.
    private SupplierService() { }

    public Guid SupplierId { get; private init; }

    public string Name { get; private set; } = null!;

    public ServiceCategory Category { get; private set; }

    public string? Description { get; private set; }

    public Money Price { get; private set; } = null!;

    public PricingUnit PricingUnit { get; private set; }

    /// <summary>How long the service lasts, e.g. 240 for a half day tour. Not relevant for accommodation.</summary>
    public int? DurationMinutes { get; private set; }

    /// <summary>Maximum number of guests per booking, when the supplier limits it.</summary>
    public int? Capacity { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    internal void Update(
        string name,
        ServiceCategory category,
        string? description,
        Money price,
        PricingUnit pricingUnit,
        int? durationMinutes,
        int? capacity)
    {
        ArgumentNullException.ThrowIfNull(price);

        Name = Guard.Required(name, "Service name", NameMaxLength);
        Category = Guard.Defined(category, "Service category");
        Description = Guard.Optional(description, "Service description", DescriptionMaxLength);
        Price = price;
        PricingUnit = Guard.Defined(pricingUnit, "Pricing unit");
        DurationMinutes = Guard.PositiveOrNull(durationMinutes, "Duration");
        Capacity = Guard.PositiveOrNull(capacity, "Capacity");
    }
}
