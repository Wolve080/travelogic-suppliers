using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Travelogic.Suppliers.Domain.Common;
using Travelogic.Suppliers.Domain.Suppliers;
using Travelogic.Suppliers.Infrastructure.Outbox;

namespace Travelogic.Suppliers.Infrastructure.Persistence;

public sealed class SuppliersDbContext(DbContextOptions<SuppliersDbContext> options, TimeProvider timeProvider) : DbContext(options)
{
    public const string Schema = "supplier";

    // rowversion, shadow property
    public const string VersionProperty = "Version";

    internal static readonly JsonSerializerOptions EventSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<SupplierService> SupplierServices => Set<SupplierService>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        var now = timeProvider.GetUtcNow();
        TouchAggregatesWithChangedServices(now);
        StampAuditFields(now);
        WriteDomainEventsToOutbox(now);

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SuppliersDbContext).Assembly);
    }

    // Services are part of the supplier aggregate, so a change to one should bump the supplier's rowversion too.
    private void TouchAggregatesWithChangedServices(DateTimeOffset now)
    {
        var changedSupplierIds = ChangeTracker.Entries<SupplierService>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.SupplierId)
            .ToHashSet();

        foreach (var entry in ChangeTracker.Entries<Supplier>())
        {
            if (entry.State == EntityState.Unchanged && changedSupplierIds.Contains(entry.Entity.Id))
            {
                entry.Property(s => s.UpdatedAtUtc).CurrentValue = now;
            }
        }
    }

    private void StampAuditFields(DateTimeOffset now)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditable.CreatedAtUtc)).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(IAuditable.UpdatedAtUtc)).CurrentValue = now;
                    break;
            }
        }
    }

    private void WriteDomainEventsToOutbox(DateTimeOffset now)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        var sequence = 0;
        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.CreateVersion7(),
                    Type = domainEvent.GetType().Name,
                    AggregateId = domainEvent.AggregateId,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), EventSerializerOptions),
                    OccurredAtUtc = now,
                    Sequence = sequence++,
                });
            }

            aggregate.ClearDomainEvents();
        }
    }
}
