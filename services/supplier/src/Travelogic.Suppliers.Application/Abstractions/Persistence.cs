using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Application.Abstractions;

/// <summary>Loads and stores whole <see cref="Supplier"/> aggregates for the write side.</summary>
public interface ISupplierRepository
{
    /// <summary>Loads the supplier together with its services, tracked for changes.</summary>
    Task<Supplier?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludeSupplierId, CancellationToken cancellationToken);

    void Add(Supplier supplier);

    void Remove(Supplier supplier);

    /// <summary>
    /// Tells persistence which version of the supplier the caller last saw, so the save fails if
    /// someone else has changed it since. Returns false if the version token is malformed.
    /// </summary>
    bool TrySetExpectedVersion(Supplier supplier, string version);
}

public interface IUnitOfWork
{
    /// <summary>Commits all pending changes, and the outbox messages they produced, in one transaction.</summary>
    /// <exception cref="Common.ConcurrencyConflictException">The aggregate changed since it was read.</exception>
    /// <exception cref="Common.UniqueConstraintException">A unique index rejected the write.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
