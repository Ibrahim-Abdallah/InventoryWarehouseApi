using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy=AuthorizationPolicies.InventoryRead)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,"application/problem+json",Description="The request failed validation.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized,"application/problem+json",Description="Authentication is required or the access token is invalid.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden,"application/problem+json",Description="The authenticated user lacks the required permission.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError,"application/problem+json",Description="An unexpected server error occurred.")]
public sealed class ReportsController(IInventoryReportingService service):ControllerBase
{
    [HttpGet("inventory-summary")][EndpointSummary("Get inventory summary report")][EndpointDescription("Returns a read-only Dapper inventory summary with database-side filtering, sorting, aggregation, and pagination.")][ProducesResponseType<PagedResult<InventorySummaryItem>>(StatusCodes.Status200OK,Description="Returns the requested inventory summary page.")] public Task<PagedResult<InventorySummaryItem>> InventorySummary([FromQuery]InventorySummaryQuery q,CancellationToken ct)=>service.ListInventorySummaryAsync(q,ct);
    [HttpGet("stock-movements")][EndpointSummary("Get stock movement report")][EndpointDescription("Returns cross-product stock movements with database-side filtering, sorting, and pagination.")][ProducesResponseType<PagedResult<StockMovementReportItem>>(StatusCodes.Status200OK,Description="Returns the requested stock movement page.")] public Task<PagedResult<StockMovementReportItem>> StockMovements([FromQuery]StockMovementReportQuery q,CancellationToken ct)=>service.ListStockMovementsAsync(q,ct);
    [HttpGet("warehouses/{warehouseId:guid}/inventory")][EndpointSummary("Get warehouse inventory report")][EndpointDescription("Returns a read-only paged inventory view for one warehouse across products and locations.")][ProducesResponseType<PagedResult<WarehouseInventoryItem>>(StatusCodes.Status200OK,Description="Returns the requested warehouse inventory page.")][ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The warehouse was not found.")] public Task<PagedResult<WarehouseInventoryItem>> WarehouseInventory(Guid warehouseId,[FromQuery]WarehouseInventoryQuery q,CancellationToken ct)=>service.ListWarehouseInventoryAsync(warehouseId,q,ct);
    [HttpGet("low-stock")][EndpointSummary("Get low-stock report")][EndpointDescription("Returns enabled low-stock positions using derived availability without changing monitoring alerts.")][ProducesResponseType<PagedResult<LowStockReportItem>>(StatusCodes.Status200OK,Description="Returns the requested low-stock report page.")] public Task<PagedResult<LowStockReportItem>> LowStock([FromQuery]LowStockReportQuery q,CancellationToken ct)=>service.ListLowStockAsync(q,ct);
    [HttpGet("products/{productId:guid}/stock-history")][EndpointSummary("Get product stock history")][EndpointDescription("Returns a signed chronological stock history for one product with UTC filtering and database-side pagination.")][ProducesResponseType<PagedResult<ProductStockHistoryItem>>(StatusCodes.Status200OK,Description="Returns the requested product history page.")][ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The product was not found.")] public Task<PagedResult<ProductStockHistoryItem>> ProductHistory(Guid productId,[FromQuery]ProductStockHistoryQuery q,CancellationToken ct)=>service.ListProductStockHistoryAsync(productId,q,ct);
}
