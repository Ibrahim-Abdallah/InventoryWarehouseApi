using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.WarehouseLocations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;using InventoryWarehouseApi.Application.Authentication;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/warehouses/{warehouseId:guid}/locations")]
[Authorize(Policy=AuthorizationPolicies.CatalogRead)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,"application/problem+json",Description="The request failed validation.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized,"application/problem+json",Description="Authentication is required or the access token is invalid.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden,"application/problem+json",Description="The authenticated user lacks the required permission.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError,"application/problem+json",Description="An unexpected server error occurred.")]
public sealed class WarehouseLocationsController(IWarehouseLocationService service) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List warehouse locations")][EndpointDescription("Returns filtered and paged locations belonging to the specified warehouse.")]
    [ProducesResponseType<PagedResult<WarehouseLocationResponse>>(StatusCodes.Status200OK,Description="Returns the requested location page.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The warehouse was not found.")]
    public Task<PagedResult<WarehouseLocationResponse>> List(Guid warehouseId, [FromQuery] WarehouseLocationQuery query, CancellationToken ct) => service.ListAsync(warehouseId, query, ct);
    [HttpGet("{locationId:guid}")]
    [EndpointSummary("Get warehouse location")][EndpointDescription("Returns one warehouse-scoped location by identifier.")]
    [ProducesResponseType<WarehouseLocationResponse>(StatusCodes.Status200OK,Description="Returns the requested location.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The warehouse or location was not found.")]
    public Task<WarehouseLocationResponse> Get(Guid warehouseId, Guid locationId, CancellationToken ct) => service.GetAsync(warehouseId, locationId, ct);
    [HttpPost]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [EndpointSummary("Create warehouse location")][EndpointDescription("Creates a warehouse location with a warehouse-scoped unique code.")]
    [ProducesResponseType<WarehouseLocationResponse>(StatusCodes.Status201Created,Description="Creates and returns the location.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The warehouse was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The location code conflicts within the warehouse.")]
    public async Task<ActionResult<WarehouseLocationResponse>> Create(Guid warehouseId, CreateWarehouseLocationRequest request, CancellationToken ct)
    {
        WarehouseLocationResponse location = await service.CreateAsync(warehouseId, request, ct);
        return CreatedAtAction(nameof(Get), new { warehouseId, locationId = location.Id }, location);
    }
    [HttpPut("{locationId:guid}")]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [EndpointSummary("Update warehouse location")][EndpointDescription("Updates warehouse location details and active status while preserving scoped code uniqueness.")]
    [ProducesResponseType<WarehouseLocationResponse>(StatusCodes.Status200OK,Description="Returns the updated location.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The warehouse or location was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The location code conflicts within the warehouse.")]
    public Task<WarehouseLocationResponse> Update(Guid warehouseId, Guid locationId, UpdateWarehouseLocationRequest request, CancellationToken ct) => service.UpdateAsync(warehouseId, locationId, request, ct);
    [HttpDelete("{locationId:guid}")]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [EndpointSummary("Delete warehouse location")][EndpointDescription("Deletes a location when no inventory dependencies prevent removal.")]
    [ProducesResponseType(StatusCodes.Status204NoContent,Description="Deletes the warehouse location.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The warehouse or location was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="Dependent records prevent deletion.")]
    public async Task<IActionResult> Delete(Guid warehouseId, Guid locationId, CancellationToken ct)
    { await service.DeleteAsync(warehouseId, locationId, ct); return NoContent(); }
}
