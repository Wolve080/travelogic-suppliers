using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Travelogic.Suppliers.Infrastructure.Outbox;

/// <summary>A domain event waiting to be published to other services.</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    /// <summary>Event name, e.g. <c>SupplierCreated</c>. Consumers route on this.</summary>
    public required string Type { get; init; }

    public Guid AggregateId { get; init; }

    /// <summary>The event serialised as JSON.</summary>
    public required string Payload { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>
    /// Position within the transaction that produced it. Events saved together share a timestamp,
    /// so this keeps them in the order they happened (e.g. SupplierCreated before SupplierServiceAdded).
    /// </summary>
    public int Sequence { get; init; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Type).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Payload).IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(2000);

        // The processor only ever looks for unprocessed messages, so index just those.
        builder.HasIndex(m => new { m.OccurredAtUtc, m.Sequence }).HasFilter("[ProcessedAtUtc] IS NULL");
    }
}
