using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Application.Suppliers;

// Public contract of the service. These types are what goes over the wire, so they are kept
// separate from the domain model: the domain can be refactored without breaking consumers.

public sealed record AddressDto(string? Line1, string? Line2, string City, string? Region, string Country, string? PostalCode);

public sealed record ContactDto(string? Email, string? Phone, string? Website);

/// <summary>A service offered by a supplier, as sent when creating or changing it.</summary>
public sealed record ServiceRequest(
    string Name,
    ServiceCategory Category,
    string? Description,
    decimal Price,
    string Currency,
    PricingUnit PricingUnit,
    int? DurationMinutes,
    int? Capacity);

/// <summary>Creates a supplier and, optionally, its services in a single request.</summary>
public sealed record CreateSupplierRequest(
    string Name,
    SupplierType Type,
    string? Description,
    ContactDto? Contact,
    AddressDto Address,
    IReadOnlyList<ServiceRequest>? Services);

/// <summary>Replaces a supplier's details. <paramref name="Version"/> is the value last read, for optimistic concurrency.</summary>
public sealed record UpdateSupplierRequest(
    string Name,
    SupplierType Type,
    string? Description,
    ContactDto? Contact,
    AddressDto Address,
    bool IsActive,
    string Version);

public sealed record ServiceResponse(
    Guid Id,
    string Name,
    ServiceCategory Category,
    string? Description,
    decimal Price,
    string Currency,
    PricingUnit PricingUnit,
    int? DurationMinutes,
    int? Capacity);

public sealed record SupplierResponse(
    Guid Id,
    string Name,
    SupplierType Type,
    string? Description,
    ContactDto Contact,
    AddressDto Address,
    bool IsActive,
    IReadOnlyList<ServiceResponse> Services,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string Version);

/// <summary>Lightweight row for the supplier list.</summary>
public sealed record SupplierSummaryResponse(
    Guid Id,
    string Name,
    SupplierType Type,
    string City,
    string Country,
    bool IsActive,
    int ServiceCount,
    IReadOnlyList<ServiceCategory> ServiceCategories,
    DateTimeOffset CreatedAtUtc);

public enum SupplierSortField
{
    Name,
    CreatedAt,
    City,
}

/// <summary>Filtering, sorting and paging for the supplier list. Out of range paging values are clamped.</summary>
public sealed record SupplierListQuery
{
    public const int MaxPageSize = 100;

    public string? Search { get; init; }

    public SupplierType? Type { get; init; }

    public bool? IsActive { get; init; }

    public SupplierSortField SortBy { get; init; } = SupplierSortField.Name;

    public bool Descending { get; init; }

    public int Page
    {
        get;
        init => field = Math.Max(1, value);
    } = 1;

    public int PageSize
    {
        get;
        init => field = Math.Clamp(value, 1, MaxPageSize);
    } = 20;
}
