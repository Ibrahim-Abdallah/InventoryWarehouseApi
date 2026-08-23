using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.WarehouseLocations;

public interface IWarehouseLocationRepository
{
    Task<bool> WarehouseExistsAsync(Guid warehouseId, CancellationToken ct);
    Task<WarehouseLocation?> GetAsync(Guid warehouseId, Guid id, bool tracking, CancellationToken ct);
    Task<bool> CodeExistsAsync(Guid warehouseId, string code, Guid? excludingId, CancellationToken ct);
    Task<bool> HasInventoryAsync(Guid warehouseId, Guid id, CancellationToken ct);
    Task<PagedResult<WarehouseLocation>> ListAsync(Guid warehouseId, WarehouseLocationQuery query, CancellationToken ct);
    Task AddAsync(WarehouseLocation location, CancellationToken ct);
    void Remove(WarehouseLocation location);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IWarehouseLocationService
{
    Task<PagedResult<WarehouseLocationResponse>> ListAsync(Guid warehouseId, WarehouseLocationQuery query, CancellationToken ct);
    Task<WarehouseLocationResponse> GetAsync(Guid warehouseId, Guid id, CancellationToken ct);
    Task<WarehouseLocationResponse> CreateAsync(Guid warehouseId, CreateWarehouseLocationRequest request, CancellationToken ct);
    Task<WarehouseLocationResponse> UpdateAsync(Guid warehouseId, Guid id, UpdateWarehouseLocationRequest request, CancellationToken ct);
    Task DeleteAsync(Guid warehouseId, Guid id, CancellationToken ct);
}
