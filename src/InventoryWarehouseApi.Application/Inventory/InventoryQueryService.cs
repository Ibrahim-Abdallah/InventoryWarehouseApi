using FluentValidation;
using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.Inventory;

internal sealed class InventoryQueryService(IInventoryQueryRepository repository,
    IValidator<LocationInventoryQuery> validator) : IInventoryQueryService
{
    public async Task<WarehouseInventoryResponse> GetWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct)
    {
        await EnsureMastersAsync(productId, warehouseId, ct);
        return await repository.GetWarehouseAsync(productId, warehouseId, ct);
    }
    public async Task<PagedResult<LocationInventoryResponse>> ListLocationsAsync(Guid productId, Guid warehouseId, LocationInventoryQuery query, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(query, ct);
        await EnsureMastersAsync(productId, warehouseId, ct);
        return await repository.ListLocationsAsync(productId, warehouseId, query, ct);
    }
    public async Task<LocationInventoryResponse> GetLocationAsync(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct)
    {
        await EnsureMastersAsync(productId, warehouseId, ct);
        if (!await repository.LocationExistsAsync(warehouseId, locationId, ct)) throw new NotFoundException("Warehouse location was not found.");
        return await repository.GetLocationAsync(productId, warehouseId, locationId, ct);
    }
    private async Task EnsureMastersAsync(Guid productId, Guid warehouseId, CancellationToken ct)
    {
        if (!await repository.ProductExistsAsync(productId, ct)) throw new NotFoundException("Product was not found.");
        if (!await repository.WarehouseExistsAsync(warehouseId, ct)) throw new NotFoundException("Warehouse was not found.");
    }
}
