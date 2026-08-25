using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.WarehouseLocations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;using InventoryWarehouseApi.Application.Authentication;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/warehouses/{warehouseId:guid}/locations")]
[Authorize(Policy=AuthorizationPolicies.CatalogRead)]
public sealed class WarehouseLocationsController(IWarehouseLocationService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<WarehouseLocationResponse>> List(Guid warehouseId, [FromQuery] WarehouseLocationQuery query, CancellationToken ct) => service.ListAsync(warehouseId, query, ct);
    [HttpGet("{locationId:guid}")]
    public Task<WarehouseLocationResponse> Get(Guid warehouseId, Guid locationId, CancellationToken ct) => service.GetAsync(warehouseId, locationId, ct);
    [HttpPost]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    public async Task<ActionResult<WarehouseLocationResponse>> Create(Guid warehouseId, CreateWarehouseLocationRequest request, CancellationToken ct)
    {
        WarehouseLocationResponse location = await service.CreateAsync(warehouseId, request, ct);
        return CreatedAtAction(nameof(Get), new { warehouseId, locationId = location.Id }, location);
    }
    [HttpPut("{locationId:guid}")]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    public Task<WarehouseLocationResponse> Update(Guid warehouseId, Guid locationId, UpdateWarehouseLocationRequest request, CancellationToken ct) => service.UpdateAsync(warehouseId, locationId, request, ct);
    [HttpDelete("{locationId:guid}")]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    public async Task<IActionResult> Delete(Guid warehouseId, Guid locationId, CancellationToken ct)
    { await service.DeleteAsync(warehouseId, locationId, ct); return NoContent(); }
}
