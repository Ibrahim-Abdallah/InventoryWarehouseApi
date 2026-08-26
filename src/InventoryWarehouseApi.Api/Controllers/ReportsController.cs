using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy=AuthorizationPolicies.InventoryRead)]
public sealed class ReportsController(IInventoryReportingService service):ControllerBase
{
    [HttpGet("inventory-summary")] public Task<PagedResult<InventorySummaryItem>> InventorySummary([FromQuery]InventorySummaryQuery q,CancellationToken ct)=>service.ListInventorySummaryAsync(q,ct);
    [HttpGet("stock-movements")] public Task<PagedResult<StockMovementReportItem>> StockMovements([FromQuery]StockMovementReportQuery q,CancellationToken ct)=>service.ListStockMovementsAsync(q,ct);
    [HttpGet("warehouses/{warehouseId:guid}/inventory")] public Task<PagedResult<WarehouseInventoryItem>> WarehouseInventory(Guid warehouseId,[FromQuery]WarehouseInventoryQuery q,CancellationToken ct)=>service.ListWarehouseInventoryAsync(warehouseId,q,ct);
    [HttpGet("low-stock")] public Task<PagedResult<LowStockReportItem>> LowStock([FromQuery]LowStockReportQuery q,CancellationToken ct)=>service.ListLowStockAsync(q,ct);
    [HttpGet("products/{productId:guid}/stock-history")] public Task<PagedResult<ProductStockHistoryItem>> ProductHistory(Guid productId,[FromQuery]ProductStockHistoryQuery q,CancellationToken ct)=>service.ListProductStockHistoryAsync(productId,q,ct);
}
