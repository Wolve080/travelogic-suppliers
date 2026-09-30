using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Travelogic.Suppliers.Application.ReferenceData;

namespace Travelogic.Suppliers.Api.Controllers;

/// <summary>Lookup values (supplier types, service categories, pricing units) for building forms.</summary>
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/reference-data")]
[Produces("application/json")]
[Tags("Reference data")]
public sealed class ReferenceDataController : ControllerBase
{
    [HttpGet]
    [EndpointName("GetReferenceData")]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType<ReferenceDataResponse>(StatusCodes.Status200OK)]
    public ActionResult<ReferenceDataResponse> Get() => Ok(ReferenceDataProvider.Get);
}
