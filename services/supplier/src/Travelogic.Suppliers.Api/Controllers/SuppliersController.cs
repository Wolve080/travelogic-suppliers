using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Travelogic.Suppliers.Api.Errors;
using Travelogic.Suppliers.Application.Common;
using Travelogic.Suppliers.Application.Suppliers;

namespace Travelogic.Suppliers.Api.Controllers;

/// <summary>Suppliers: the businesses that provide tourism services.</summary>
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/suppliers")]
[Produces("application/json")]
[Tags("Suppliers")]
public sealed class SuppliersController(SupplierCommands commands, ISupplierQueries queries) : ControllerBase
{
    /// <summary>Lists suppliers, with optional search, filtering, sorting and paging.</summary>
    [HttpGet]
    [EndpointName("ListSuppliers")]
    [ProducesResponseType<PagedResult<SupplierSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SupplierSummaryResponse>>> List([FromQuery] SupplierListQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.ListAsync(query, cancellationToken));

    /// <summary>Gets a supplier and all of its services.</summary>
    [HttpGet("{id:guid}", Name = RouteNames.GetSupplier)]
    [EndpointName("GetSupplier")]
    [ProducesResponseType<SupplierResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<SupplierResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        await queries.GetByIdAsync(id, cancellationToken) is { } supplier
            ? Ok(supplier)
            : this.Problem(SupplierErrors.NotFound(id));

    /// <summary>Creates a supplier, optionally together with its services.</summary>
    [HttpPost]
    [EndpointName("CreateSupplier")]
    [Consumes("application/json")]
    [ProducesResponseType<SupplierResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<SupplierResponse>> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await commands.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.Problem(result.Error!);
        }

        var created = await queries.GetByIdAsync(result.Value, cancellationToken);
        return CreatedAtRoute(RouteNames.GetSupplier, new { id = result.Value, version = "1" }, created);
    }

    /// <summary>Replaces a supplier's details. Services are managed through their own endpoints.</summary>
    /// <remarks>Send the <c>version</c> from your last read. If someone else changed the supplier since, you get a 409.</remarks>
    [HttpPut("{id:guid}")]
    [EndpointName("UpdateSupplier")]
    [Consumes("application/json")]
    [ProducesResponseType<SupplierResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<SupplierResponse>> Update(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await commands.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess
            ? Ok(await queries.GetByIdAsync(id, cancellationToken))
            : this.Problem(result.Error!);
    }

    /// <summary>Deletes a supplier and its services.</summary>
    [HttpDelete("{id:guid}")]
    [EndpointName("DeleteSupplier")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await commands.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.Problem(result.Error!);
    }
}

internal static class RouteNames
{
    public const string GetSupplier = nameof(GetSupplier);
    public const string GetService = nameof(GetService);
}
