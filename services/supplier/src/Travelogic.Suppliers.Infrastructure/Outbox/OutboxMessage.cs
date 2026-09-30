using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Travelogic.Suppliers.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    public required string Type { get; init; }

    public Guid AggregateId { get; init; }

    public required string Payload { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }

    // events from the same SaveChanges share OccurredAtUtc, this keeps their order
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

        builder.HasIndex(m => new { m.OccurredAtUtc, m.Sequence }).HasFilter("[ProcessedAtUtc] IS NULL");
    }
}
