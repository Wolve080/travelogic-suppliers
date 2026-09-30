using Travelogic.Suppliers.Domain.Common;
using Travelogic.Suppliers.Domain.Suppliers;
using Travelogic.Suppliers.Domain.Suppliers.Events;

namespace Travelogic.Suppliers.UnitTests.Domain;

public class SupplierTests
{
    private static Supplier NewSupplier(string name = "Kruger Horizons Safaris") =>
        Supplier.Create(
            name,
            SupplierType.Safari,
            "Game drives",
            ContactDetails.Create("info@example.com", null, null),
            Address.Create(null, null, "Hazyview", null, "South Africa", null));

    private static SupplierService AddGameDrive(Supplier supplier, string name = "Half Day Game Drive") =>
        supplier.AddService(name, ServiceCategory.Activity, null, Money.Of(1450m, "ZAR"), PricingUnit.PerPerson, 240, 9);

    [Fact]
    public void Create_sets_details_and_raises_SupplierCreated()
    {
        var supplier = NewSupplier("  Kruger Horizons Safaris  ");

        supplier.Id.ShouldNotBe(Guid.Empty);
        supplier.Name.ShouldBe("Kruger Horizons Safaris");
        supplier.IsActive.ShouldBeTrue();
        supplier.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SupplierCreated>().AggregateId.ShouldBe(supplier.Id);
    }

    [Fact]
    public void Create_uses_time_ordered_version_7_ids()
    {
        NewSupplier().Id.Version.ShouldBe(7);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_requires_a_name(string? name)
    {
        Should.Throw<DomainException>(() => NewSupplier(name!)).Message.ShouldContain("name is required");
    }

    [Fact]
    public void Create_rejects_undefined_supplier_type()
    {
        Should.Throw<DomainException>(() => Supplier.Create(
            "X", (SupplierType)42, null, ContactDetails.Create(null, null, null), Address.Create(null, null, "Durban", null, "South Africa", null)));
    }

    [Fact]
    public void AddService_adds_the_service_and_raises_event()
    {
        var supplier = NewSupplier();
        supplier.ClearDomainEvents();

        var service = AddGameDrive(supplier);

        supplier.Services.ShouldHaveSingleItem().ShouldBe(service);
        service.SupplierId.ShouldBe(supplier.Id);
        service.Price.ShouldBe(Money.Of(1450m, "ZAR"));
        supplier.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SupplierServiceAdded>().ServiceId.ShouldBe(service.Id);
    }

    [Fact]
    public void AddService_rejects_a_duplicate_name_ignoring_case_and_whitespace()
    {
        var supplier = NewSupplier();
        AddGameDrive(supplier, "Half Day Game Drive");

        Should.Throw<DomainException>(() => AddGameDrive(supplier, "  half day game drive "));
    }

    [Fact]
    public void AddService_rejects_non_positive_duration()
    {
        var supplier = NewSupplier();

        Should.Throw<DomainException>(() =>
            supplier.AddService("Walk", ServiceCategory.Activity, null, Money.Of(10, "ZAR"), PricingUnit.PerPerson, 0, null));
    }

    [Fact]
    public void UpdateService_allows_keeping_the_same_name()
    {
        var supplier = NewSupplier();
        var service = AddGameDrive(supplier);

        supplier.UpdateService(service.Id, "Half Day Game Drive", ServiceCategory.Tour, "Updated", Money.Of(1500m, "zar"), PricingUnit.PerGroup, 300, 6);

        service.Category.ShouldBe(ServiceCategory.Tour);
        service.Price.Currency.ShouldBe("ZAR");
        service.Capacity.ShouldBe(6);
    }

    [Fact]
    public void UpdateService_rejects_a_name_used_by_another_service()
    {
        var supplier = NewSupplier();
        AddGameDrive(supplier, "Morning Drive");
        var evening = AddGameDrive(supplier, "Evening Drive");

        Should.Throw<DomainException>(() =>
            supplier.UpdateService(evening.Id, "Morning Drive", ServiceCategory.Activity, null, Money.Of(1, "ZAR"), PricingUnit.PerPerson, null, null));
    }

    [Fact]
    public void RemoveService_removes_it_and_raises_event()
    {
        var supplier = NewSupplier();
        var service = AddGameDrive(supplier);
        supplier.ClearDomainEvents();

        supplier.RemoveService(service.Id);

        supplier.Services.ShouldBeEmpty();
        supplier.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SupplierServiceRemoved>();
    }

    [Fact]
    public void RemoveService_throws_for_a_service_of_another_supplier()
    {
        Should.Throw<DomainException>(() => NewSupplier().RemoveService(Guid.NewGuid()));
    }

    [Fact]
    public void UpdateDetails_changes_details_and_raises_event()
    {
        var supplier = NewSupplier();
        supplier.ClearDomainEvents();

        supplier.UpdateDetails("New Name", SupplierType.TourOperator, null, ContactDetails.Create(null, null, null),
            Address.Create(null, null, "Nelspruit", null, "South Africa", null), isActive: false);

        supplier.Name.ShouldBe("New Name");
        supplier.IsActive.ShouldBeFalse();
        supplier.Address.City.ShouldBe("Nelspruit");
        supplier.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SupplierUpdated>();
    }
}
