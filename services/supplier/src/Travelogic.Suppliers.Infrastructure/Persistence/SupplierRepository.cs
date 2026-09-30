using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Travelogic.Suppliers.Application.Abstractions;
using Travelogic.Suppliers.Application.Common;
using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Infrastructure.Persistence;

internal sealed class SupplierRepository(SuppliersDbContext db) : ISupplierRepository
{
    public Task<Supplier?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Suppliers
            .Include(s => s.Services)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludeSupplierId, CancellationToken cancellationToken) =>
        db.Suppliers.AnyAsync(s => s.Name == name && s.Id != excludeSupplierId, cancellationToken);

    public void Add(Supplier supplier) => db.Suppliers.Add(supplier);

    public void Remove(Supplier supplier) => db.Suppliers.Remove(supplier);

    public bool TrySetExpectedVersion(Supplier supplier, string version)
    {
        Span<byte> buffer = stackalloc byte[8];
        if (!Convert.TryFromBase64String(version, buffer, out var written) || written != 8)
        {
            return false;
        }

        // EF compares the original value with the row's current rowversion in the UPDATE's WHERE clause.
        db.Entry(supplier).Property<byte[]>(SuppliersDbContext.VersionProperty).OriginalValue = buffer.ToArray();
        return true;
    }
}

internal sealed class UnitOfWork(SuppliersDbContext db) : IUnitOfWork
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The aggregate was modified by another request.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation })
        {
            throw new UniqueConstraintException("A unique index rejected the write.", ex);
        }
    }
}
