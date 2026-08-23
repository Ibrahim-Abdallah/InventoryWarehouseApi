using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Application.Inventory;

public interface IInventoryQueryRepository
{
    Task<bool> ProductExistsAsync(Guid id, CancellationToken ct);
    Task<bool> WarehouseExistsAsync(Guid id, CancellationToken ct);
    Task<bool> LocationExistsAsync(Guid warehouseId, Guid id, CancellationToken ct);
    Task<WarehouseInventoryResponse> GetWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct);
    Task<PagedResult<LocationInventoryResponse>> ListLocationsAsync(Guid productId, Guid warehouseId, LocationInventoryQuery query, CancellationToken ct);
    Task<LocationInventoryResponse> GetLocationAsync(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct);
}

public interface IInventoryQueryService
{
    Task<WarehouseInventoryResponse> GetWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct);
    Task<PagedResult<LocationInventoryResponse>> ListLocationsAsync(Guid productId, Guid warehouseId, LocationInventoryQuery query, CancellationToken ct);
    Task<LocationInventoryResponse> GetLocationAsync(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct);
}

public interface IStockMovementRepository
{
    Task<bool> ProductExistsAsync(Guid id, CancellationToken ct);
    Task<bool> WarehouseExistsAsync(Guid id, CancellationToken ct);
    Task<bool> LocationExistsAsync(Guid warehouseId, Guid id, CancellationToken ct);
    Task<(StockMovement Movement, InventoryBalance Balance)> ExecuteAsync(Guid productId, Guid warehouseId,
        Guid locationId, StockMovementType movementType, decimal quantity, string? referenceType,
        string? referenceId, DateTimeOffset occurredAtUtc, CancellationToken ct);
    Task<PagedResult<StockMovement>> ListAsync(Guid productId, Guid warehouseId, Guid locationId,
        StockMovementHistoryQuery query, CancellationToken ct);
}

public interface IStockMovementService
{
    Task<StockMovementOperationResponse> StockInAsync(Guid productId, Guid warehouseId, Guid locationId, StockMovementRequest request, CancellationToken ct);
    Task<StockMovementOperationResponse> StockOutAsync(Guid productId, Guid warehouseId, Guid locationId, StockMovementRequest request, CancellationToken ct);
    Task<PagedResult<StockMovementResponse>> ListAsync(Guid productId, Guid warehouseId, Guid locationId, StockMovementHistoryQuery query, CancellationToken ct);
}
