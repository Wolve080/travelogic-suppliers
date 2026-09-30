using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Application.Abstractions;

public interface ISupplierRepository
{
    Task<Supplier?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludeSupplierId, CancellationToken cancellationToken);

    void Add(Supplier supplier);

    void Remove(Supplier supplier);

    // Returns false if the version token is not valid.
    bool TrySetExpectedVersion(Supplier supplier, string version);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
