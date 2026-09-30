using Travelogic.Suppliers.Domain.Common;

namespace Travelogic.Suppliers.Domain.Suppliers;

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

    // EF Core
    private SupplierService() { }

    public Guid SupplierId { get; private init; }

    public string Name { get; private set; } = null!;

    public ServiceCategory Category { get; private set; }

    public string? Description { get; private set; }

    public Money Price { get; private set; } = null!;

    public PricingUnit PricingUnit { get; private set; }

    // e.g. 240 for a half day tour
    public int? DurationMinutes { get; private set; }

    // max guests
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
