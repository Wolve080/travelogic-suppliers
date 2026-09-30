using FluentValidation;
using Microsoft.Extensions.Logging;
using Travelogic.Suppliers.Application.Abstractions;
using Travelogic.Suppliers.Application.Common;
using Travelogic.Suppliers.Domain.Common;
using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Application.Suppliers;

public sealed partial class SupplierCommands(
    ISupplierRepository repository,
    IUnitOfWork unitOfWork,
    IValidator<CreateSupplierRequest> createValidator,
    IValidator<UpdateSupplierRequest> updateValidator,
    IValidator<ServiceRequest> serviceValidator,
    ILogger<SupplierCommands> logger)
{
    public async Task<Result<Guid>> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        if (await createValidator.ValidateToErrorAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        if (await repository.NameExistsAsync(request.Name.Trim(), excludeSupplierId: null, cancellationToken))
        {
            return SupplierErrors.DuplicateName(request.Name);
        }

        Supplier supplier;
        try
        {
            supplier = Supplier.Create(request.Name, request.Type, request.Description, ToContact(request.Contact), ToAddress(request.Address));
            foreach (var service in request.Services ?? [])
            {
                AddService(supplier, service);
            }
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        repository.Add(supplier);

        if (await SaveAsync(() => SupplierErrors.DuplicateName(request.Name), cancellationToken) is { } failed)
        {
            return failed;
        }

        LogSupplierCreated(supplier.Id, supplier.Name, supplier.Services.Count);
        return supplier.Id;
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        if (await updateValidator.ValidateToErrorAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var supplier = await repository.GetAsync(id, cancellationToken);
        if (supplier is null)
        {
            return SupplierErrors.NotFound(id);
        }

        if (!repository.TrySetExpectedVersion(supplier, request.Version))
        {
            return SupplierErrors.InvalidVersion();
        }

        if (await repository.NameExistsAsync(request.Name.Trim(), excludeSupplierId: id, cancellationToken))
        {
            return SupplierErrors.DuplicateName(request.Name);
        }

        try
        {
            supplier.UpdateDetails(request.Name, request.Type, request.Description, ToContact(request.Contact), ToAddress(request.Address), request.IsActive);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        if (await SaveAsync(() => SupplierErrors.DuplicateName(request.Name), cancellationToken) is { } failed)
        {
            return failed;
        }

        LogSupplierUpdated(id);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await repository.GetAsync(id, cancellationToken);
        if (supplier is null)
        {
            return SupplierErrors.NotFound(id);
        }

        supplier.MarkDeleted();
        repository.Remove(supplier);

        if (await SaveAsync(SupplierErrors.ConcurrencyConflict, cancellationToken) is { } failed)
        {
            return failed;
        }

        LogSupplierDeleted(id);
        return Result.Success();
    }

    public async Task<Result<Guid>> AddServiceAsync(Guid supplierId, ServiceRequest request, CancellationToken cancellationToken)
    {
        if (await serviceValidator.ValidateToErrorAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var supplier = await repository.GetAsync(supplierId, cancellationToken);
        if (supplier is null)
        {
            return SupplierErrors.NotFound(supplierId);
        }

        if (HasServiceNamed(supplier, request.Name, excludeServiceId: null))
        {
            return SupplierErrors.DuplicateServiceName(request.Name);
        }

        SupplierService service;
        try
        {
            service = AddService(supplier, request);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        if (await SaveAsync(() => SupplierErrors.DuplicateServiceName(request.Name), cancellationToken) is { } failed)
        {
            return failed;
        }

        LogServiceAdded(service.Id, supplierId);
        return service.Id;
    }

    public async Task<Result> UpdateServiceAsync(Guid supplierId, Guid serviceId, ServiceRequest request, CancellationToken cancellationToken)
    {
        if (await serviceValidator.ValidateToErrorAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var supplier = await repository.GetAsync(supplierId, cancellationToken);
        if (supplier is null)
        {
            return SupplierErrors.NotFound(supplierId);
        }

        if (supplier.FindService(serviceId) is null)
        {
            return SupplierErrors.ServiceNotFound(supplierId, serviceId);
        }

        if (HasServiceNamed(supplier, request.Name, excludeServiceId: serviceId))
        {
            return SupplierErrors.DuplicateServiceName(request.Name);
        }

        try
        {
            supplier.UpdateService(
                serviceId,
                request.Name,
                request.Category,
                request.Description,
                Money.Of(request.Price, request.Currency),
                request.PricingUnit,
                request.DurationMinutes,
                request.Capacity);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        if (await SaveAsync(() => SupplierErrors.DuplicateServiceName(request.Name), cancellationToken) is { } failed)
        {
            return failed;
        }

        LogServiceUpdated(serviceId, supplierId);
        return Result.Success();
    }

    public async Task<Result> RemoveServiceAsync(Guid supplierId, Guid serviceId, CancellationToken cancellationToken)
    {
        var supplier = await repository.GetAsync(supplierId, cancellationToken);
        if (supplier is null)
        {
            return SupplierErrors.NotFound(supplierId);
        }

        if (supplier.FindService(serviceId) is null)
        {
            return SupplierErrors.ServiceNotFound(supplierId, serviceId);
        }

        supplier.RemoveService(serviceId);

        if (await SaveAsync(SupplierErrors.ConcurrencyConflict, cancellationToken) is { } failed)
        {
            return failed;
        }

        LogServiceRemoved(serviceId, supplierId);
        return Result.Success();
    }

    private static SupplierService AddService(Supplier supplier, ServiceRequest request) =>
        supplier.AddService(
            request.Name,
            request.Category,
            request.Description,
            Money.Of(request.Price, request.Currency),
            request.PricingUnit,
            request.DurationMinutes,
            request.Capacity);

    private static bool HasServiceNamed(Supplier supplier, string name, Guid? excludeServiceId) =>
        supplier.Services.Any(s => s.Id != excludeServiceId && string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

    private static ContactDetails ToContact(ContactDto? contact) =>
        ContactDetails.Create(contact?.Email, contact?.Phone, contact?.Website);

    private static Address ToAddress(AddressDto address) =>
        Address.Create(address.Line1, address.Line2, address.City, address.Region, address.Country, address.PostalCode);

    private async Task<Error?> SaveAsync(Func<Error> onUniqueViolation, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (ConcurrencyConflictException)
        {
            return SupplierErrors.ConcurrencyConflict();
        }
        catch (UniqueConstraintException)
        {
            return onUniqueViolation();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created supplier {SupplierId} ({SupplierName}) with {ServiceCount} services")]
    private partial void LogSupplierCreated(Guid supplierId, string supplierName, int serviceCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated supplier {SupplierId}")]
    private partial void LogSupplierUpdated(Guid supplierId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted supplier {SupplierId}")]
    private partial void LogSupplierDeleted(Guid supplierId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Added service {ServiceId} to supplier {SupplierId}")]
    private partial void LogServiceAdded(Guid serviceId, Guid supplierId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated service {ServiceId} on supplier {SupplierId}")]
    private partial void LogServiceUpdated(Guid serviceId, Guid supplierId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Removed service {ServiceId} from supplier {SupplierId}")]
    private partial void LogServiceRemoved(Guid serviceId, Guid supplierId);
}
