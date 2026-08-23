using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/inventory/products/{productId:guid}/warehouses/{warehouseId:guid}")]
public sealed class InventoryController(IInventoryQueryService service) : ControllerBase
{
    [HttpGet]
    public Task<WarehouseInventoryResponse> GetWarehouse(Guid productId, Guid warehouseId, CancellationToken ct) => service.GetWarehouseAsync(productId, warehouseId, ct);
    [HttpGet("locations")]
    public Task<PagedResult<LocationInventoryResponse>> ListLocations(Guid productId, Guid warehouseId, [FromQuery] LocationInventoryQuery query, CancellationToken ct) => service.ListLocationsAsync(productId, warehouseId, query, ct);
    [HttpGet("locations/{locationId:guid}")]
    public Task<LocationInventoryResponse> GetLocation(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct) => service.GetLocationAsync(productId, warehouseId, locationId, ct);
}
