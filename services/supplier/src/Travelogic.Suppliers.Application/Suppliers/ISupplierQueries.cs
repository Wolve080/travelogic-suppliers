using Travelogic.Suppliers.Application.Common;

namespace Travelogic.Suppliers.Application.Suppliers;

public interface ISupplierQueries
{
    Task<PagedResult<SupplierSummaryResponse>> ListAsync(SupplierListQuery query, CancellationToken cancellationToken);

    Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
