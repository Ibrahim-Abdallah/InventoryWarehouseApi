using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Application.Inventory;

internal sealed class InventoryAdjustmentService(IInventoryAdjustmentRepository repository,
    IValidator<InventoryAdjustmentRequest> requestValidator,
    IValidator<InventoryAdjustmentHistoryQuery> queryValidator) : IInventoryAdjustmentService
{
    public Task<InventoryAdjustmentOperationResponse> IncreaseAsync(Guid productId, Guid warehouseId,
        Guid locationId, InventoryAdjustmentRequest request, CancellationToken ct) =>
        ExecuteAsync(productId, warehouseId, locationId, InventoryAdjustmentType.Increase, request, ct);

    public Task<InventoryAdjustmentOperationResponse> DecreaseAsync(Guid productId, Guid warehouseId,
        Guid locationId, InventoryAdjustmentRequest request, CancellationToken ct) =>
        ExecuteAsync(productId, warehouseId, locationId, InventoryAdjustmentType.Decrease, request, ct);

    public async Task<PagedResult<InventoryAdjustmentResponse>> ListAsync(Guid productId, Guid warehouseId,
        Guid locationId, InventoryAdjustmentHistoryQuery query, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);
        await EnsurePositionAsync(productId, warehouseId, locationId, ct);
        PagedResult<InventoryAdjustment> page = await repository.ListAsync(productId, warehouseId, locationId, query, ct);
        return new(page.Items.Select(Map).ToList(), page.PageNumber, page.PageSize, page.TotalCount);
    }

    private async Task<InventoryAdjustmentOperationResponse> ExecuteAsync(Guid productId, Guid warehouseId,
        Guid locationId, InventoryAdjustmentType type, InventoryAdjustmentRequest request, CancellationToken ct)
    {
        await requestValidator.ValidateAndThrowAsync(request, ct);
        var result = await repository.ExecuteAsync(productId, warehouseId, locationId, type, request.Quantity,
            request.Reason, request.AdjustedBy, DateTimeOffset.UtcNow, ct);
        return new(result.Adjustment.Id, result.Adjustment.AdjustmentType, result.Adjustment.ProductId,
            result.Adjustment.WarehouseId, result.Adjustment.WarehouseLocationId, result.Adjustment.Quantity,
            result.Adjustment.Reason, result.Adjustment.AdjustedBy, result.Adjustment.StockMovementId,
            result.Movement.MovementType, result.Adjustment.AdjustedAtUtc, result.Balance.OnHandQuantity,
            result.Balance.ReservedQuantity, result.Balance.AvailableQuantity);
    }

    private async Task EnsurePositionAsync(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct)
    {
        if (!await repository.ProductExistsAsync(productId, ct)) throw new NotFoundException("Product was not found.");
        if (!await repository.WarehouseExistsAsync(warehouseId, ct)) throw new NotFoundException("Warehouse was not found.");
        if (!await repository.LocationExistsAsync(warehouseId, locationId, ct)) throw new NotFoundException("Warehouse location was not found.");
    }

    private static InventoryAdjustmentResponse Map(InventoryAdjustment x) => new(x.Id, x.ProductId,
        x.WarehouseId, x.WarehouseLocationId, x.AdjustmentType, x.Quantity, x.Reason, x.AdjustedBy,
        x.StockMovementId, x.AdjustedAtUtc);
}
