using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Application.Suppliers;

public sealed record AddressDto(string? Line1, string? Line2, string City, string? Region, string Country, string? PostalCode);

public sealed record ContactDto(string? Email, string? Phone, string? Website);

public sealed record ServiceRequest(
    string Name,
    ServiceCategory Category,
    string? Description,
    decimal Price,
    string Currency,
    PricingUnit PricingUnit,
    int? DurationMinutes,
    int? Capacity);

public sealed record CreateSupplierRequest(
    string Name,
    SupplierType Type,
    string? Description,
    ContactDto? Contact,
    AddressDto Address,
    IReadOnlyList<ServiceRequest>? Services);

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
