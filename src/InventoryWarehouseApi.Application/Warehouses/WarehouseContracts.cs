using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.Warehouses;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string normalizedCode, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> HasLocationsAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<Warehouse>> ListAsync(WarehouseQuery query, CancellationToken cancellationToken);
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken);
    void Remove(Warehouse warehouse);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IWarehouseService
{
    Task<PagedResult<WarehouseResponse>> ListAsync(WarehouseQuery query, CancellationToken cancellationToken);
    Task<WarehouseResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<WarehouseResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken);
    Task<WarehouseResponse> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
