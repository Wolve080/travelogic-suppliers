using Microsoft.EntityFrameworkCore;
using Travelogic.Suppliers.Application.Common;
using Travelogic.Suppliers.Application.Suppliers;
using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Infrastructure.Persistence;

internal sealed class SupplierQueries(SuppliersDbContext db) : ISupplierQueries
{
    public async Task<PagedResult<SupplierSummaryResponse>> ListAsync(SupplierListQuery query, CancellationToken cancellationToken)
    {
        var suppliers = db.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            suppliers = suppliers.Where(s =>
                s.Name.Contains(term) || s.Address.City.Contains(term) || s.Address.Country.Contains(term));
        }

        if (query.Type is { } type)
        {
            suppliers = suppliers.Where(s => s.Type == type);
        }

        if (query.IsActive is { } isActive)
        {
            suppliers = suppliers.Where(s => s.IsActive == isActive);
        }

        var total = await suppliers.CountAsync(cancellationToken);

        var items = await Sort(suppliers, query.SortBy, query.Descending)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new SupplierSummaryResponse(
                s.Id,
                s.Name,
                s.Type,
                s.Address.City,
                s.Address.Country,
                s.IsActive,
                s.Services.Count,
                s.Services.Select(x => x.Category).Distinct().ToList(),
                s.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierSummaryResponse>(items, query.Page, query.PageSize, total);
    }

    public async Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await db.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Type,
                s.Description,
                Contact = new ContactDto(s.Contact.Email, s.Contact.Phone, s.Contact.Website),
                Address = new AddressDto(s.Address.Line1, s.Address.Line2, s.Address.City, s.Address.Region, s.Address.Country, s.Address.PostalCode),
                s.IsActive,
                Services = s.Services
                    .OrderBy(x => x.Name)
                    .Select(x => new ServiceResponse(
                        x.Id,
                        x.Name,
                        x.Category,
                        x.Description,
                        x.Price.Amount,
                        x.Price.Currency,
                        x.PricingUnit,
                        x.DurationMinutes,
                        x.Capacity))
                    .ToList(),
                s.CreatedAtUtc,
                s.UpdatedAtUtc,
                Version = EF.Property<byte[]>(s, SuppliersDbContext.VersionProperty),
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new SupplierResponse(
                row.Id,
                row.Name,
                row.Type,
                row.Description,
                row.Contact,
                row.Address,
                row.IsActive,
                row.Services,
                row.CreatedAtUtc,
                row.UpdatedAtUtc,
                Convert.ToBase64String(row.Version));
    }

    private static IQueryable<Supplier> Sort(IQueryable<Supplier> suppliers, SupplierSortField sortBy, bool descending) =>
        (sortBy, descending) switch
        {
            (SupplierSortField.CreatedAt, false) => suppliers.OrderBy(s => s.CreatedAtUtc).ThenBy(s => s.Id),
            (SupplierSortField.CreatedAt, true) => suppliers.OrderByDescending(s => s.CreatedAtUtc).ThenBy(s => s.Id),
            (SupplierSortField.City, false) => suppliers.OrderBy(s => s.Address.City).ThenBy(s => s.Name),
            (SupplierSortField.City, true) => suppliers.OrderByDescending(s => s.Address.City).ThenBy(s => s.Name),
            (_, true) => suppliers.OrderByDescending(s => s.Name),
            _ => suppliers.OrderBy(s => s.Name),
        };
}
