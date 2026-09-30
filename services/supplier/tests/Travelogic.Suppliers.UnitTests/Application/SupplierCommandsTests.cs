using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Travelogic.Suppliers.Application.Abstractions;
using Travelogic.Suppliers.Application.Common;
using Travelogic.Suppliers.Application.Suppliers;
using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.UnitTests.Application;

public class SupplierCommandsTests
{
    private readonly ISupplierRepository _repository = Substitute.For<ISupplierRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SupplierCommands _commands;

    public SupplierCommandsTests()
    {
        _commands = new SupplierCommands(
            _repository,
            _unitOfWork,
            new CreateSupplierRequestValidator(),
            new UpdateSupplierRequestValidator(),
            new ServiceRequestValidator(),
            NullLogger<SupplierCommands>.Instance);

        _repository.TrySetExpectedVersion(Arg.Any<Supplier>(), Arg.Any<string>()).Returns(true);
    }

    private static ServiceRequest GameDrive(string name = "Half Day Game Drive") =>
        new(name, ServiceCategory.Activity, "Morning drive", 1450m, "ZAR", PricingUnit.PerPerson, 240, 9);

    private static CreateSupplierRequest ValidCreate(params ServiceRequest[] services) =>
        new(
            "Kruger Horizons Safaris",
            SupplierType.Safari,
            null,
            new ContactDto("bookings@example.com", "+27 13 555 0199", "https://example.com"),
            new AddressDto(null, null, "Hazyview", "Mpumalanga", "South Africa", "1242"),
            services);

    private static Supplier ExistingSupplier()
    {
        var supplier = Supplier.Create("Existing", SupplierType.Safari, null, ContactDetails.Create(null, null, null),
            Address.Create(null, null, "Hazyview", null, "South Africa", null));
        supplier.ClearDomainEvents();
        return supplier;
    }

    [Fact]
    public async Task Create_saves_the_supplier_with_its_services()
    {
        Supplier? added = null;
        _repository.Add(Arg.Do<Supplier>(s => added = s));

        var result = await _commands.CreateAsync(ValidCreate(GameDrive(), GameDrive("Full Day Safari")), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        added.ShouldNotBeNull();
        added.Id.ShouldBe(result.Value);
        added.Services.Count.ShouldBe(2);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_returns_every_validation_problem_with_json_paths()
    {
        var request = ValidCreate(GameDrive() with { Price = -5, Currency = "RANDS" }) with
        {
            Name = "",
            Address = new AddressDto(null, null, "", null, "", null),
            Contact = new ContactDto("not-an-email", null, "ftp://example.com"),
        };

        var result = await _commands.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.Details.Keys.ShouldBe(
            ["name", "address.city", "address.country", "contact.email", "contact.website", "services[0].price", "services[0].currency"],
            ignoreOrder: true);
        _repository.DidNotReceive().Add(Arg.Any<Supplier>());
    }

    [Fact]
    public async Task Create_rejects_duplicate_service_names_in_the_request()
    {
        var result = await _commands.CreateAsync(ValidCreate(GameDrive("Game Drive"), GameDrive("game drive")), CancellationToken.None);

        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.Details.ShouldContainKey("services");
    }

    [Fact]
    public async Task Create_returns_conflict_when_the_name_is_taken()
    {
        _repository.NameExistsAsync("Kruger Horizons Safaris", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _commands.CreateAsync(ValidCreate(), CancellationToken.None);

        result.Error!.Code.ShouldBe("supplier_name_taken");
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public async Task Create_maps_a_unique_index_race_to_conflict()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new UniqueConstraintException());

        var result = await _commands.CreateAsync(ValidCreate(), CancellationToken.None);

        result.Error!.Code.ShouldBe("supplier_name_taken");
    }

    [Fact]
    public async Task Update_returns_not_found_for_unknown_supplier()
    {
        var id = Guid.NewGuid();
        var request = new UpdateSupplierRequest("Name", SupplierType.Safari, null, null,
            new AddressDto(null, null, "Hazyview", null, "South Africa", null), true, "AAAAAAAAB9E=");

        var result = await _commands.UpdateAsync(id, request, CancellationToken.None);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Update_returns_conflict_when_the_supplier_changed_since_it_was_read()
    {
        var supplier = ExistingSupplier();
        _repository.GetAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new ConcurrencyConflictException());
        var request = new UpdateSupplierRequest("Renamed", SupplierType.Safari, null, null,
            new AddressDto(null, null, "Hazyview", null, "South Africa", null), true, "AAAAAAAAB9E=");

        var result = await _commands.UpdateAsync(supplier.Id, request, CancellationToken.None);

        result.Error!.Code.ShouldBe("concurrency_conflict");
    }

    [Fact]
    public async Task Update_rejects_a_malformed_version()
    {
        var supplier = ExistingSupplier();
        _repository.GetAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);
        _repository.TrySetExpectedVersion(supplier, "garbage").Returns(false);
        var request = new UpdateSupplierRequest("Renamed", SupplierType.Safari, null, null,
            new AddressDto(null, null, "Hazyview", null, "South Africa", null), true, "garbage");

        var result = await _commands.UpdateAsync(supplier.Id, request, CancellationToken.None);

        result.Error!.Details.ShouldContainKey("version");
    }

    [Fact]
    public async Task AddService_returns_conflict_for_a_duplicate_name()
    {
        var supplier = ExistingSupplier();
        supplier.AddService("Game Drive", ServiceCategory.Activity, null, Money.Of(1, "ZAR"), PricingUnit.PerPerson, null, null);
        _repository.GetAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        var result = await _commands.AddServiceAsync(supplier.Id, GameDrive("GAME DRIVE"), CancellationToken.None);

        result.Error!.Code.ShouldBe("service_name_taken");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveService_returns_not_found_for_unknown_service()
    {
        var supplier = ExistingSupplier();
        _repository.GetAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        var result = await _commands.RemoveServiceAsync(supplier.Id, Guid.NewGuid(), CancellationToken.None);

        result.Error!.Code.ShouldBe("service_not_found");
    }

    [Fact]
    public async Task Delete_marks_the_supplier_deleted_and_removes_it()
    {
        var supplier = ExistingSupplier();
        _repository.GetAsync(supplier.Id, Arg.Any<CancellationToken>()).Returns(supplier);

        var result = await _commands.DeleteAsync(supplier.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _repository.Received(1).Remove(supplier);
        supplier.DomainEvents.ShouldHaveSingleItem();
    }
}
