using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Travelogic.Suppliers.Api.Errors;
using Travelogic.Suppliers.Application.Suppliers;

namespace Travelogic.Suppliers.Api.Controllers;

/// <summary>The services (accommodation, activities, tours, ...) a supplier offers.</summary>
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/suppliers/{supplierId:guid}/services")]
[Produces("application/json")]
[Tags("Supplier services")]
public sealed class SupplierServicesController(SupplierCommands commands, ISupplierQueries queries) : ControllerBase
{
    /// <summary>Lists a supplier's services.</summary>
    [HttpGet]
    [EndpointName("ListSupplierServices")]
    [ProducesResponseType<IReadOnlyList<ServiceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<ServiceResponse>>> List(Guid supplierId, CancellationToken cancellationToken) =>
        await queries.GetByIdAsync(supplierId, cancellationToken) is { } supplier
            ? Ok(supplier.Services)
            : this.Problem(SupplierErrors.NotFound(supplierId));

    /// <summary>Gets one of a supplier's services.</summary>
    [HttpGet("{serviceId:guid}", Name = RouteNames.GetService)]
    [EndpointName("GetSupplierService")]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ServiceResponse>> Get(Guid supplierId, Guid serviceId, CancellationToken cancellationToken)
    {
        var supplier = await queries.GetByIdAsync(supplierId, cancellationToken);
        if (supplier is null)
        {
            return this.Problem(SupplierErrors.NotFound(supplierId));
        }

        return supplier.Services.FirstOrDefault(s => s.Id == serviceId) is { } service
            ? Ok(service)
            : this.Problem(SupplierErrors.ServiceNotFound(supplierId, serviceId));
    }

    /// <summary>Adds a service to a supplier.</summary>
    [HttpPost]
    [EndpointName("AddSupplierService")]
    [Consumes("application/json")]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<ServiceResponse>> Add(Guid supplierId, ServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await commands.AddServiceAsync(supplierId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.Problem(result.Error!);
        }

        var supplier = await queries.GetByIdAsync(supplierId, cancellationToken);
        var service = supplier?.Services.FirstOrDefault(s => s.Id == result.Value);
        return CreatedAtRoute(RouteNames.GetService, new { supplierId, serviceId = result.Value, version = "1" }, service);
    }

    /// <summary>Replaces a service's details.</summary>
    [HttpPut("{serviceId:guid}")]
    [EndpointName("UpdateSupplierService")]
    [Consumes("application/json")]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<ServiceResponse>> Update(Guid supplierId, Guid serviceId, ServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await commands.UpdateServiceAsync(supplierId, serviceId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.Problem(result.Error!);
        }

        var supplier = await queries.GetByIdAsync(supplierId, cancellationToken);
        return Ok(supplier?.Services.FirstOrDefault(s => s.Id == serviceId));
    }

    /// <summary>Removes a service from a supplier.</summary>
    [HttpDelete("{serviceId:guid}")]
    [EndpointName("RemoveSupplierService")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Remove(Guid supplierId, Guid serviceId, CancellationToken cancellationToken)
    {
        var result = await commands.RemoveServiceAsync(supplierId, serviceId, cancellationToken);
        return result.IsSuccess ? NoContent() : this.Problem(result.Error!);
    }
}
