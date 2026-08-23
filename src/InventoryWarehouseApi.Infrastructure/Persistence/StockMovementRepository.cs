using System.Data;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

internal sealed class StockMovementRepository(InventoryWarehouseDbContext dbContext) : IStockMovementRepository
{
    public Task<bool> ProductExistsAsync(Guid id, CancellationToken ct) => dbContext.Products.AnyAsync(x => x.Id == id, ct);
    public Task<bool> WarehouseExistsAsync(Guid id, CancellationToken ct) => dbContext.Warehouses.AnyAsync(x => x.Id == id, ct);
    public Task<bool> LocationExistsAsync(Guid warehouseId, Guid id, CancellationToken ct) =>
        dbContext.WarehouseLocations.AnyAsync(x => x.WarehouseId == warehouseId && x.Id == id, ct);

    public async Task<(StockMovement Movement, InventoryBalance Balance)> ExecuteAsync(Guid productId,
        Guid warehouseId, Guid locationId, StockMovementType movementType, decimal quantity,
        string? referenceType, string? referenceId, DateTimeOffset occurredAtUtc, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        if (!await ProductExistsAsync(productId, ct)) throw new NotFoundException("Product was not found.");
        if (!await WarehouseExistsAsync(warehouseId, ct)) throw new NotFoundException("Warehouse was not found.");
        if (!await LocationExistsAsync(warehouseId, locationId, ct)) throw new NotFoundException("Warehouse location was not found.");

        InventoryBalance? balance = await dbContext.InventoryBalances.SingleOrDefaultAsync(x =>
            x.ProductId == productId && x.WarehouseId == warehouseId && x.WarehouseLocationId == locationId, ct);

        if (movementType == StockMovementType.StockIn)
        {
            if (balance is null)
            {
                balance = new InventoryBalance(productId, warehouseId, locationId, quantity, 0m);
                dbContext.InventoryBalances.Add(balance);
            }
            else balance.Receive(quantity);
        }
        else
        {
            if (balance is null) throw new ConflictException("Insufficient available stock.");
            try { balance.Issue(quantity); }
            catch (InvalidOperationException) { throw new ConflictException("Insufficient available stock."); }
        }

        StockMovement movement = new(productId, warehouseId, locationId, movementType, quantity,
            referenceType, referenceId, occurredAtUtc);
        dbContext.StockMovements.Add(movement);
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (movement, balance);
    }

    public async Task<PagedResult<StockMovement>> ListAsync(Guid productId, Guid warehouseId,
        Guid locationId, StockMovementHistoryQuery query, CancellationToken ct)
    {
        IQueryable<StockMovement> movements = dbContext.StockMovements.AsNoTracking().Where(x =>
            x.ProductId == productId && x.WarehouseId == warehouseId && x.WarehouseLocationId == locationId);
        int count = await movements.CountAsync(ct);
        int offset = (query.PageNumber - 1) * query.PageSize;
        List<StockMovement> items = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
            ? await dbContext.StockMovements.FromSqlInterpolated($"""
                SELECT * FROM StockMovements
                WHERE ProductId = {productId} AND WarehouseId = {warehouseId} AND WarehouseLocationId = {locationId}
                ORDER BY OccurredAtUtc DESC, Id DESC
                LIMIT {query.PageSize} OFFSET {offset}
                """).AsNoTracking().ToListAsync(ct)
            : await movements.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                .Skip(offset).Take(query.PageSize).ToListAsync(ct);
        return new(items, query.PageNumber, query.PageSize, count);
    }
}
