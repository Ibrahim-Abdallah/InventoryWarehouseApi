using InventoryWarehouseApi.Application.Common;using InventoryWarehouseApi.Application.LowStock;using Microsoft.AspNetCore.Mvc;
namespace InventoryWarehouseApi.Api.Controllers;
[ApiController]
public sealed class LowStockController(ILowStockService service):ControllerBase
{
 [HttpPut("api/low-stock-thresholds/products/{productId:guid}/warehouses/{warehouseId:guid}/locations/{locationId:guid}")] public Task<LowStockThresholdResponse> Upsert(Guid productId,Guid warehouseId,Guid locationId,UpsertLowStockThresholdRequest request,CancellationToken ct)=>service.UpsertThresholdAsync(productId,warehouseId,locationId,request,ct);
 [HttpGet("api/low-stock-thresholds/products/{productId:guid}/warehouses/{warehouseId:guid}/locations/{locationId:guid}")] public Task<LowStockThresholdResponse> Get(Guid productId,Guid warehouseId,Guid locationId,CancellationToken ct)=>service.GetThresholdAsync(productId,warehouseId,locationId,ct);
 [HttpGet("api/low-stock-thresholds")] public Task<PagedResult<LowStockThresholdResponse>> Thresholds([FromQuery]LowStockThresholdQuery q,CancellationToken ct)=>service.ListThresholdsAsync(q,ct);
 [HttpGet("api/low-stock")] public Task<PagedResult<LowStockResponse>> Low([FromQuery]LowStockQuery q,CancellationToken ct)=>service.ListLowStockAsync(q,ct);
 [HttpGet("api/low-stock-alerts")] public Task<PagedResult<LowStockAlertResponse>> Alerts([FromQuery]LowStockAlertQuery q,CancellationToken ct)=>service.ListAlertsAsync(q,ct);
}
