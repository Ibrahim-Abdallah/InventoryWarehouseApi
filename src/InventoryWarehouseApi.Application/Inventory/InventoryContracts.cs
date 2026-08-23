using InventoryWarehouseApi.Application.Common;

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
