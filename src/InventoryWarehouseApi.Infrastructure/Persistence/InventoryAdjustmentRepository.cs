using System.Data;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

internal sealed class InventoryAdjustmentRepository(InventoryWarehouseDbContext dbContext) : IInventoryAdjustmentRepository
{
    public Task<bool> ProductExistsAsync(Guid id, CancellationToken ct) => dbContext.Products.AnyAsync(x => x.Id == id, ct);
    public Task<bool> WarehouseExistsAsync(Guid id, CancellationToken ct) => dbContext.Warehouses.AnyAsync(x => x.Id == id, ct);
    public Task<bool> LocationExistsAsync(Guid warehouseId, Guid id, CancellationToken ct) =>
        dbContext.WarehouseLocations.AnyAsync(x => x.WarehouseId == warehouseId && x.Id == id, ct);

    public async Task<(InventoryAdjustment Adjustment, StockMovement Movement, InventoryBalance Balance)> ExecuteAsync(
        Guid productId, Guid warehouseId, Guid locationId, InventoryAdjustmentType adjustmentType,
        decimal quantity, string reason, string adjustedBy, DateTimeOffset adjustedAtUtc, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await ProductExistsAsync(productId, ct)) throw new NotFoundException("Product was not found.");
        if (!await WarehouseExistsAsync(warehouseId, ct)) throw new NotFoundException("Warehouse was not found.");
        if (!await LocationExistsAsync(warehouseId, locationId, ct)) throw new NotFoundException("Warehouse location was not found.");

        InventoryBalance? balance = await dbContext.FindInventoryBalanceForUpdateAsync(
            productId, warehouseId, locationId, ct);
        if (adjustmentType == InventoryAdjustmentType.Increase)
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
            if (balance is null) throw new ConflictException("No inventory balance exists for this adjustment.");
            try { balance.Issue(quantity); }
            catch (InvalidOperationException) { throw new ConflictException("Insufficient available stock for adjustment."); }
        }

        Guid adjustmentId = Guid.NewGuid();
        Guid movementId = Guid.NewGuid();
        StockMovementType movementType = adjustmentType == InventoryAdjustmentType.Increase
            ? StockMovementType.AdjustmentIncrease : StockMovementType.AdjustmentDecrease;
        StockMovement movement = new(movementId, productId, warehouseId, locationId, movementType, quantity,
            "InventoryAdjustment", adjustmentId.ToString("D"), adjustedAtUtc);
        InventoryAdjustment adjustment = new(adjustmentId, productId, warehouseId, locationId,
            adjustmentType, quantity, reason, adjustedBy, movementId, adjustedAtUtc);
        dbContext.StockMovements.Add(movement);
        dbContext.InventoryAdjustments.Add(adjustment);
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (adjustment, movement, balance);
    }

    public async Task<PagedResult<InventoryAdjustment>> ListAsync(Guid productId, Guid warehouseId,
        Guid locationId, InventoryAdjustmentHistoryQuery query, CancellationToken ct)
    {
        IQueryable<InventoryAdjustment> adjustments = dbContext.InventoryAdjustments.AsNoTracking().Where(x =>
            x.ProductId == productId && x.WarehouseId == warehouseId && x.WarehouseLocationId == locationId);
        int count = await adjustments.CountAsync(ct);
        int offset = (query.PageNumber - 1) * query.PageSize;
        List<InventoryAdjustment> items = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
            ? await dbContext.InventoryAdjustments.FromSqlInterpolated($"""
                SELECT * FROM InventoryAdjustments
                WHERE ProductId = {productId} AND WarehouseId = {warehouseId} AND WarehouseLocationId = {locationId}
                ORDER BY AdjustedAtUtc DESC, Id DESC
                LIMIT {query.PageSize} OFFSET {offset}
                """).AsNoTracking().ToListAsync(ct)
            : await adjustments.OrderByDescending(x => x.AdjustedAtUtc).ThenByDescending(x => x.Id)
                .Skip(offset).Take(query.PageSize).ToListAsync(ct);
        return new(items, query.PageNumber, query.PageSize, count);
    }
}
