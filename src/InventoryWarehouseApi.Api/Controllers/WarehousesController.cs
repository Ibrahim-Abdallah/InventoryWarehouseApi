using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Warehouses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;using InventoryWarehouseApi.Application.Authentication;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize(Policy=AuthorizationPolicies.CatalogRead)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,"application/problem+json",Description="The request failed validation.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized,"application/problem+json",Description="Authentication is required or the access token is invalid.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden,"application/problem+json",Description="The authenticated user lacks the required permission.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError,"application/problem+json",Description="An unexpected server error occurred.")]
public sealed class WarehousesController(IWarehouseService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<WarehouseResponse>>(StatusCodes.Status200OK)]
    [EndpointSummary("List warehouses")][EndpointDescription("Returns a filtered, sorted, and paged warehouse catalog.")]
    public Task<PagedResult<WarehouseResponse>> List([FromQuery] WarehouseQuery query, CancellationToken ct) => service.ListAsync(query, ct);
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get warehouse")][EndpointDescription("Returns one warehouse by identifier.")]
    [ProducesResponseType<WarehouseResponse>(StatusCodes.Status200OK,Description="Returns the requested warehouse.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The warehouse was not found.")]
    public Task<WarehouseResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpPost]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [ProducesResponseType<WarehouseResponse>(StatusCodes.Status201Created)]
    [EndpointSummary("Create warehouse")][EndpointDescription("Creates a warehouse with a normalized, case-insensitive unique code.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The code conflicts with an existing warehouse.")]
    public async Task<ActionResult<WarehouseResponse>> Create(CreateWarehouseRequest request, CancellationToken ct)
    {
        WarehouseResponse warehouse = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = warehouse.Id }, warehouse);
    }
    [HttpPut("{id:guid}")]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [EndpointSummary("Update warehouse")][EndpointDescription("Updates warehouse catalog details and active status while preserving code uniqueness.")]
    [ProducesResponseType<WarehouseResponse>(StatusCodes.Status200OK,Description="Returns the updated warehouse.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The warehouse was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The code conflicts with an existing warehouse.")]
    public Task<WarehouseResponse> Update(Guid id, UpdateWarehouseRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);
    [HttpDelete("{id:guid}")]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Delete warehouse")][EndpointDescription("Deletes a warehouse when no locations or inventory dependencies prevent removal.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The warehouse was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="Dependent records prevent deletion.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
