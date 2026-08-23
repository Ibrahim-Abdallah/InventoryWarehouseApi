using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/inventory/products/{productId:guid}/warehouses/{warehouseId:guid}")]
public sealed class InventoryController(IInventoryQueryService service, IStockMovementService movementService) : ControllerBase
{
    [HttpGet]
    public Task<WarehouseInventoryResponse> GetWarehouse(Guid productId, Guid warehouseId, CancellationToken ct) => service.GetWarehouseAsync(productId, warehouseId, ct);
    [HttpGet("locations")]
    public Task<PagedResult<LocationInventoryResponse>> ListLocations(Guid productId, Guid warehouseId, [FromQuery] LocationInventoryQuery query, CancellationToken ct) => service.ListLocationsAsync(productId, warehouseId, query, ct);
    [HttpGet("locations/{locationId:guid}")]
    public Task<LocationInventoryResponse> GetLocation(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct) => service.GetLocationAsync(productId, warehouseId, locationId, ct);
    [HttpPost("locations/{locationId:guid}/stock-in")]
    public Task<StockMovementOperationResponse> StockIn(Guid productId, Guid warehouseId, Guid locationId,
        StockMovementRequest request, CancellationToken ct) => movementService.StockInAsync(productId, warehouseId, locationId, request, ct);
    [HttpPost("locations/{locationId:guid}/stock-out")]
    public Task<StockMovementOperationResponse> StockOut(Guid productId, Guid warehouseId, Guid locationId,
        StockMovementRequest request, CancellationToken ct) => movementService.StockOutAsync(productId, warehouseId, locationId, request, ct);
    [HttpGet("locations/{locationId:guid}/movements")]
    public Task<PagedResult<StockMovementResponse>> ListMovements(Guid productId, Guid warehouseId, Guid locationId,
        [FromQuery] StockMovementHistoryQuery query, CancellationToken ct) => movementService.ListAsync(productId, warehouseId, locationId, query, ct);
}
