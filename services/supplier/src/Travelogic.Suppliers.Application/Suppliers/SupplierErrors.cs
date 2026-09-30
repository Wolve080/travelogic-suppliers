using Travelogic.Suppliers.Application.Common;

namespace Travelogic.Suppliers.Application.Suppliers;

public static class SupplierErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("supplier_not_found", $"Supplier {id} was not found.");

    public static Error ServiceNotFound(Guid supplierId, Guid serviceId) =>
        Error.NotFound("service_not_found", $"Service {serviceId} was not found on supplier {supplierId}.");

    public static Error DuplicateName(string name) =>
        Error.Conflict("supplier_name_taken", $"A supplier called \"{name.Trim()}\" already exists.");

    public static Error DuplicateServiceName(string name) =>
        Error.Conflict("service_name_taken", $"This supplier already has a service called \"{name.Trim()}\".");

    public static Error ConcurrencyConflict() =>
        Error.Conflict("concurrency_conflict", "The supplier was changed by someone else. Reload it and try again.");

    public static Error InvalidVersion() =>
        Error.Validation("The version token is not valid.", new Dictionary<string, string[]> { ["version"] = ["The version token is not valid."] });
}
