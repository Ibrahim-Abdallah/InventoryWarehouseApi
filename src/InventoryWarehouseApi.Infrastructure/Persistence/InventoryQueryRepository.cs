using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Inventory;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

internal sealed class InventoryQueryRepository(InventoryWarehouseDbContext dbContext) : IInventoryQueryRepository
{
    public Task<bool> ProductExistsAsync(Guid id, CancellationToken ct) => dbContext.Products.AnyAsync(x => x.Id == id, ct);
    public Task<bool> WarehouseExistsAsync(Guid id, CancellationToken ct) => dbContext.Warehouses.AnyAsync(x => x.Id == id, ct);
    public Task<bool> LocationExistsAsync(Guid warehouseId, Guid id, CancellationToken ct) => dbContext.WarehouseLocations.AnyAsync(x => x.WarehouseId == warehouseId && x.Id == id, ct);
    public async Task<WarehouseInventoryResponse> GetWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct)
    {
        var totals = await dbContext.InventoryBalances.AsNoTracking().Where(x => x.ProductId == productId && x.WarehouseId == warehouseId)
            .GroupBy(_ => 1).Select(g => new { OnHand = g.Sum(x => x.OnHandQuantity), Reserved = g.Sum(x => x.ReservedQuantity) })
            .SingleOrDefaultAsync(ct);
        decimal onHand = totals?.OnHand ?? 0m, reserved = totals?.Reserved ?? 0m;
        return new(productId, warehouseId, onHand, reserved, onHand - reserved);
    }
    public async Task<PagedResult<LocationInventoryResponse>> ListLocationsAsync(Guid productId, Guid warehouseId, LocationInventoryQuery q, CancellationToken ct)
    {
        var query = dbContext.WarehouseLocations.AsNoTracking().Where(l => l.WarehouseId == warehouseId)
            .GroupJoin(dbContext.InventoryBalances.AsNoTracking().Where(b => b.ProductId == productId && b.WarehouseId == warehouseId),
                l => new { l.WarehouseId, LocationId = l.Id }, b => new { b.WarehouseId, LocationId = b.WarehouseLocationId },
                (l, balances) => new { Location = l, Balance = balances.FirstOrDefault() });
        if (!string.IsNullOrWhiteSpace(q.Search)) { string search = q.Search.Trim(); query = query.Where(x => x.Location.Code.Contains(search) || x.Location.Name.Contains(search)); }
        if (q.IsActive.HasValue) query = query.Where(x => x.Location.IsActive == q.IsActive.Value);
        int count = await query.CountAsync(ct);
        bool desc = q.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        query = (q.SortBy?.ToLowerInvariant(), desc) switch
        {
            ("name", false) => query.OrderBy(x => x.Location.Name).ThenBy(x => x.Location.Id),
            ("name", true) => query.OrderByDescending(x => x.Location.Name).ThenBy(x => x.Location.Id),
            ("code", true) => query.OrderByDescending(x => x.Location.Code).ThenBy(x => x.Location.Id),
            _ => query.OrderBy(x => x.Location.Code).ThenBy(x => x.Location.Id)
        };
        List<LocationInventoryResponse> items = await query.Skip((q.PageNumber - 1) * q.PageSize).Take(q.PageSize)
            .Select(x => new LocationInventoryResponse(productId, warehouseId, x.Location.Id, x.Location.Code, x.Location.Name,
                x.Location.IsActive, x.Balance == null ? 0m : x.Balance.OnHandQuantity, x.Balance == null ? 0m : x.Balance.ReservedQuantity,
                x.Balance == null ? 0m : x.Balance.OnHandQuantity - x.Balance.ReservedQuantity)).ToListAsync(ct);
        return new(items, q.PageNumber, q.PageSize, count);
    }
    public Task<LocationInventoryResponse> GetLocationAsync(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct) =>
        dbContext.WarehouseLocations.AsNoTracking().Where(l => l.WarehouseId == warehouseId && l.Id == locationId)
            .GroupJoin(dbContext.InventoryBalances.AsNoTracking().Where(b => b.ProductId == productId && b.WarehouseId == warehouseId),
                l => new { l.WarehouseId, LocationId = l.Id }, b => new { b.WarehouseId, LocationId = b.WarehouseLocationId },
                (l, balances) => new { Location = l, Balance = balances.FirstOrDefault() })
            .Select(x => new LocationInventoryResponse(productId, warehouseId, x.Location.Id, x.Location.Code, x.Location.Name,
                x.Location.IsActive, x.Balance == null ? 0m : x.Balance.OnHandQuantity, x.Balance == null ? 0m : x.Balance.ReservedQuantity,
                x.Balance == null ? 0m : x.Balance.OnHandQuantity - x.Balance.ReservedQuantity)).SingleAsync(ct);
}
