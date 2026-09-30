using Travelogic.Suppliers.Application.Common;

namespace Travelogic.Suppliers.Application.Suppliers;

/// <summary>
/// Read side. Queries project straight from the database into response shapes, skipping the domain
/// model: reads never change state, so they do not need the aggregate's rules or change tracking.
/// </summary>
public interface ISupplierQueries
{
    Task<PagedResult<SupplierSummaryResponse>> ListAsync(SupplierListQuery query, CancellationToken cancellationToken);

    Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
