using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Infrastructure.Persistence.Configurations;

internal sealed class SupplierServiceConfiguration : IEntityTypeConfiguration<SupplierService>
{
    public void Configure(EntityTypeBuilder<SupplierService> builder)
    {
        builder.ToTable("SupplierServices", table =>
        {
            table.HasCheckConstraint("CK_SupplierServices_Price", "[Price] >= 0");
            table.HasCheckConstraint("CK_SupplierServices_DurationMinutes", "[DurationMinutes] IS NULL OR [DurationMinutes] > 0");
            table.HasCheckConstraint("CK_SupplierServices_Capacity", "[Capacity] IS NULL OR [Capacity] > 0");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(SupplierService.NameMaxLength).IsRequired();
        builder.Property(s => s.Category).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.Description).HasMaxLength(SupplierService.DescriptionMaxLength);
        builder.Property(s => s.PricingUnit).HasConversion<string>().HasMaxLength(50);

        builder.ComplexProperty(s => s.Price, price =>
        {
            price.Property(p => p.Amount).HasColumnName("Price").HasPrecision(18, 2);
            price.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3).IsFixedLength().IsUnicode(false);
        });

        builder.HasIndex(s => new { s.SupplierId, s.Name }).IsUnique();
        builder.HasIndex(s => s.Category);
    }
}
