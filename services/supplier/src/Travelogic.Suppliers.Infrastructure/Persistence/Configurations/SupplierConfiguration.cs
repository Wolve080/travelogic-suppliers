using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(Supplier.NameMaxLength).IsRequired();
        builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.Description).HasMaxLength(Supplier.DescriptionMaxLength);
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.CreatedAtUtc);
        builder.Property(s => s.UpdatedAtUtc);

        builder.Property<byte[]>(SuppliersDbContext.VersionProperty).IsRowVersion();

        builder.ComplexProperty(s => s.Contact, contact =>
        {
            contact.Property(c => c.Email).HasColumnName("ContactEmail").HasMaxLength(256);
            contact.Property(c => c.Phone).HasColumnName("ContactPhone").HasMaxLength(30);
            contact.Property(c => c.Website).HasColumnName("ContactWebsite").HasMaxLength(256);
        });

        builder.ComplexProperty(s => s.Address, address =>
        {
            address.Property(a => a.Line1).HasColumnName("AddressLine1").HasMaxLength(200);
            address.Property(a => a.Line2).HasColumnName("AddressLine2").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100).IsRequired();
            address.Property(a => a.Region).HasColumnName("Region").HasMaxLength(100);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100).IsRequired();
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
        });

        builder.HasMany(s => s.Services)
            .WithOne()
            .HasForeignKey(s => s.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Services)
            .HasField("_services")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(s => s.DomainEvents);

        builder.HasIndex(s => s.Name).IsUnique();
        builder.HasIndex(s => s.Type);
        builder.HasIndex(s => s.CreatedAtUtc);
    }
}
