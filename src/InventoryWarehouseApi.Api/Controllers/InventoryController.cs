using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;using InventoryWarehouseApi.Application.Authentication;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/inventory/products/{productId:guid}/warehouses/{warehouseId:guid}")]
[Authorize(Policy=AuthorizationPolicies.InventoryRead)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,"application/problem+json",Description="The request failed validation.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized,"application/problem+json",Description="Authentication is required or the access token is invalid.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden,"application/problem+json",Description="The authenticated user lacks the required permission.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError,"application/problem+json",Description="An unexpected server error occurred.")]
public sealed class InventoryController(IInventoryQueryService service, IStockMovementService movementService,
    IInventoryAdjustmentService adjustmentService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Get warehouse inventory")][EndpointDescription("Returns the product inventory totals aggregated across locations in the requested warehouse.")]
    [ProducesResponseType<WarehouseInventoryResponse>(StatusCodes.Status200OK,Description="Returns the requested warehouse inventory.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The product or warehouse was not found.")]
    public Task<WarehouseInventoryResponse> GetWarehouse(Guid productId, Guid warehouseId, CancellationToken ct) => service.GetWarehouseAsync(productId, warehouseId, ct);
    [HttpGet("locations")]
    [EndpointSummary("List location inventory")][EndpointDescription("Returns paged location-level balances for a product in a warehouse, including locations without a balance.")]
    [ProducesResponseType<PagedResult<LocationInventoryResponse>>(StatusCodes.Status200OK,Description="Returns the requested page of location inventory.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The product or warehouse was not found.")]
    public Task<PagedResult<LocationInventoryResponse>> ListLocations(Guid productId, Guid warehouseId, [FromQuery] LocationInventoryQuery query, CancellationToken ct) => service.ListLocationsAsync(productId, warehouseId, query, ct);
    [HttpGet("locations/{locationId:guid}")]
    [EndpointSummary("Get location inventory")][EndpointDescription("Returns the exact product, warehouse, and location balance with derived available quantity.")]
    [ProducesResponseType<LocationInventoryResponse>(StatusCodes.Status200OK,Description="Returns the requested inventory position.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The requested inventory position was not found.")]
    public Task<LocationInventoryResponse> GetLocation(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct) => service.GetLocationAsync(productId, warehouseId, locationId, ct);
    [HttpPost("locations/{locationId:guid}/stock-in")]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    [EndpointSummary("Record stock in")][EndpointDescription("Adds physical stock to the exact inventory position and creates an immutable StockIn movement.")]
    [ProducesResponseType<StockMovementOperationResponse>(StatusCodes.Status200OK,Description="Returns the updated balance and StockIn movement.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The product, warehouse, or location was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The request conflicts with current inventory state.")]
    public Task<StockMovementOperationResponse> StockIn(Guid productId, Guid warehouseId, Guid locationId,
        StockMovementRequest request, CancellationToken ct) => movementService.StockInAsync(productId, warehouseId, locationId, request, ct);
    [HttpPost("locations/{locationId:guid}/stock-out")]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    [EndpointSummary("Record stock out")][EndpointDescription("Removes physical stock from the exact position when sufficient AvailableQuantity exists. Reserved stock cannot be consumed.")]
    [ProducesResponseType<StockMovementOperationResponse>(StatusCodes.Status200OK,Description="Returns the updated balance and StockOut movement.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The requested inventory position was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="Available quantity is insufficient or current state conflicts.")]
    public Task<StockMovementOperationResponse> StockOut(Guid productId, Guid warehouseId, Guid locationId,
        StockMovementRequest request, CancellationToken ct) => movementService.StockOutAsync(productId, warehouseId, locationId, request, ct);
    [HttpGet("locations/{locationId:guid}/movements")]
    [EndpointSummary("List stock movements")][EndpointDescription("Returns newest-first immutable physical movement history for the exact inventory position.")]
    [ProducesResponseType<PagedResult<StockMovementResponse>>(StatusCodes.Status200OK,Description="Returns the requested movement page.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The requested inventory position was not found.")]
    public Task<PagedResult<StockMovementResponse>> ListMovements(Guid productId, Guid warehouseId, Guid locationId,
        [FromQuery] StockMovementHistoryQuery query, CancellationToken ct) => movementService.ListAsync(productId, warehouseId, locationId, query, ct);
    [HttpPost("locations/{locationId:guid}/adjustments/increase")]
    [Authorize(Policy=AuthorizationPolicies.InventoryAdjust)]
    [EndpointSummary("Increase inventory adjustment")][EndpointDescription("Applies an audited inventory increase to the exact position and creates a linked AdjustmentIncrease stock movement.")]
    [ProducesResponseType<InventoryAdjustmentOperationResponse>(StatusCodes.Status200OK,Description="Returns the adjustment, movement, and updated balance.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The product, warehouse, or location was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The request conflicts with current inventory state.")]
    public Task<InventoryAdjustmentOperationResponse> IncreaseAdjustment(Guid productId, Guid warehouseId,
        Guid locationId, InventoryAdjustmentRequest request, CancellationToken ct) =>
        adjustmentService.IncreaseAsync(productId, warehouseId, locationId, request, ct);
    [HttpPost("locations/{locationId:guid}/adjustments/decrease")]
    [Authorize(Policy=AuthorizationPolicies.InventoryAdjust)]
    [EndpointSummary("Decrease inventory adjustment")][EndpointDescription("Applies an audited decrease when sufficient AvailableQuantity exists and creates a linked AdjustmentDecrease movement.")]
    [ProducesResponseType<InventoryAdjustmentOperationResponse>(StatusCodes.Status200OK,Description="Returns the adjustment, movement, and updated balance.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The requested inventory position was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="Available quantity is insufficient or current state conflicts.")]
    public Task<InventoryAdjustmentOperationResponse> DecreaseAdjustment(Guid productId, Guid warehouseId,
        Guid locationId, InventoryAdjustmentRequest request, CancellationToken ct) =>
        adjustmentService.DecreaseAsync(productId, warehouseId, locationId, request, ct);
    [HttpGet("locations/{locationId:guid}/adjustments")]
    [EndpointSummary("List inventory adjustments")][EndpointDescription("Returns newest-first audited adjustment history for the exact inventory position.")]
    [ProducesResponseType<PagedResult<InventoryAdjustmentResponse>>(StatusCodes.Status200OK,Description="Returns the requested adjustment page.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The requested inventory position was not found.")]
    public Task<PagedResult<InventoryAdjustmentResponse>> ListAdjustments(Guid productId, Guid warehouseId,
        Guid locationId, [FromQuery] InventoryAdjustmentHistoryQuery query, CancellationToken ct) =>
        adjustmentService.ListAsync(productId, warehouseId, locationId, query, ct);
}
