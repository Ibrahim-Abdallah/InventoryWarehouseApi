using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;using InventoryWarehouseApi.Application.Authentication;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/inventory/products/{productId:guid}/warehouses/{warehouseId:guid}")]
[Authorize(Policy=AuthorizationPolicies.InventoryRead)]
public sealed class InventoryController(IInventoryQueryService service, IStockMovementService movementService,
    IInventoryAdjustmentService adjustmentService) : ControllerBase
{
    [HttpGet]
    public Task<WarehouseInventoryResponse> GetWarehouse(Guid productId, Guid warehouseId, CancellationToken ct) => service.GetWarehouseAsync(productId, warehouseId, ct);
    [HttpGet("locations")]
    public Task<PagedResult<LocationInventoryResponse>> ListLocations(Guid productId, Guid warehouseId, [FromQuery] LocationInventoryQuery query, CancellationToken ct) => service.ListLocationsAsync(productId, warehouseId, query, ct);
    [HttpGet("locations/{locationId:guid}")]
    public Task<LocationInventoryResponse> GetLocation(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct) => service.GetLocationAsync(productId, warehouseId, locationId, ct);
    [HttpPost("locations/{locationId:guid}/stock-in")]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    public Task<StockMovementOperationResponse> StockIn(Guid productId, Guid warehouseId, Guid locationId,
        StockMovementRequest request, CancellationToken ct) => movementService.StockInAsync(productId, warehouseId, locationId, request, ct);
    [HttpPost("locations/{locationId:guid}/stock-out")]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    public Task<StockMovementOperationResponse> StockOut(Guid productId, Guid warehouseId, Guid locationId,
        StockMovementRequest request, CancellationToken ct) => movementService.StockOutAsync(productId, warehouseId, locationId, request, ct);
    [HttpGet("locations/{locationId:guid}/movements")]
    public Task<PagedResult<StockMovementResponse>> ListMovements(Guid productId, Guid warehouseId, Guid locationId,
        [FromQuery] StockMovementHistoryQuery query, CancellationToken ct) => movementService.ListAsync(productId, warehouseId, locationId, query, ct);
    [HttpPost("locations/{locationId:guid}/adjustments/increase")]
    [Authorize(Policy=AuthorizationPolicies.InventoryAdjust)]
    public Task<InventoryAdjustmentOperationResponse> IncreaseAdjustment(Guid productId, Guid warehouseId,
        Guid locationId, InventoryAdjustmentRequest request, CancellationToken ct) =>
        adjustmentService.IncreaseAsync(productId, warehouseId, locationId, request, ct);
    [HttpPost("locations/{locationId:guid}/adjustments/decrease")]
    [Authorize(Policy=AuthorizationPolicies.InventoryAdjust)]
    public Task<InventoryAdjustmentOperationResponse> DecreaseAdjustment(Guid productId, Guid warehouseId,
        Guid locationId, InventoryAdjustmentRequest request, CancellationToken ct) =>
        adjustmentService.DecreaseAsync(productId, warehouseId, locationId, request, ct);
    [HttpGet("locations/{locationId:guid}/adjustments")]
    public Task<PagedResult<InventoryAdjustmentResponse>> ListAdjustments(Guid productId, Guid warehouseId,
        Guid locationId, [FromQuery] InventoryAdjustmentHistoryQuery query, CancellationToken ct) =>
        adjustmentService.ListAsync(productId, warehouseId, locationId, query, ct);
}
