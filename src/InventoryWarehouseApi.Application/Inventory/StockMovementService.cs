using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Application.Inventory;

internal sealed class StockMovementService(IStockMovementRepository repository,
    IValidator<StockMovementRequest> requestValidator,
    IValidator<StockMovementHistoryQuery> queryValidator) : IStockMovementService
{
    public Task<StockMovementOperationResponse> StockInAsync(Guid productId, Guid warehouseId,
        Guid locationId, StockMovementRequest request, CancellationToken ct) =>
        ExecuteAsync(productId, warehouseId, locationId, StockMovementType.StockIn, request, ct);

    public Task<StockMovementOperationResponse> StockOutAsync(Guid productId, Guid warehouseId,
        Guid locationId, StockMovementRequest request, CancellationToken ct) =>
        ExecuteAsync(productId, warehouseId, locationId, StockMovementType.StockOut, request, ct);

    public async Task<PagedResult<StockMovementResponse>> ListAsync(Guid productId, Guid warehouseId,
        Guid locationId, StockMovementHistoryQuery query, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);
        await EnsurePositionAsync(productId, warehouseId, locationId, ct);
        PagedResult<StockMovement> page = await repository.ListAsync(productId, warehouseId, locationId, query, ct);
        return new(page.Items.Select(Map).ToList(), page.PageNumber, page.PageSize, page.TotalCount);
    }

    private async Task<StockMovementOperationResponse> ExecuteAsync(Guid productId, Guid warehouseId,
        Guid locationId, StockMovementType type, StockMovementRequest request, CancellationToken ct)
    {
        await requestValidator.ValidateAndThrowAsync(request, ct);
        var result = await repository.ExecuteAsync(productId, warehouseId, locationId, type,
            request.Quantity, request.ReferenceType, request.ReferenceId, DateTimeOffset.UtcNow, ct);
        StockMovement movement = result.Movement;
        InventoryBalance balance = result.Balance;
        return new(movement.Id, movement.MovementType, movement.ProductId, movement.WarehouseId,
            movement.WarehouseLocationId, movement.Quantity, movement.ReferenceType, movement.ReferenceId,
            movement.OccurredAtUtc, balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity);
    }

    private async Task EnsurePositionAsync(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct)
    {
        if (!await repository.ProductExistsAsync(productId, ct)) throw new NotFoundException("Product was not found.");
        if (!await repository.WarehouseExistsAsync(warehouseId, ct)) throw new NotFoundException("Warehouse was not found.");
        if (!await repository.LocationExistsAsync(warehouseId, locationId, ct)) throw new NotFoundException("Warehouse location was not found.");
    }

    private static StockMovementResponse Map(StockMovement x) => new(x.Id, x.ProductId, x.WarehouseId,
        x.WarehouseLocationId, x.MovementType, x.Quantity, x.ReferenceType, x.ReferenceId, x.OccurredAtUtc);
}
