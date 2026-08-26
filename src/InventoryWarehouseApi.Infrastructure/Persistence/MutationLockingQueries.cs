using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

internal static class MutationLockingQueries
{
    public static Task<InventoryBalance?> FindInventoryBalanceForUpdateAsync(
        this InventoryWarehouseDbContext dbContext, Guid productId, Guid warehouseId, Guid locationId,
        CancellationToken ct)
    {
        if (!UsesSqlServerWriterLocks(dbContext.Database.ProviderName))
            return dbContext.InventoryBalances.SingleOrDefaultAsync(x => x.ProductId == productId &&
                x.WarehouseId == warehouseId && x.WarehouseLocationId == locationId, ct);

        return dbContext.InventoryBalances.FromSqlInterpolated($"""
            SELECT * FROM [InventoryBalances] WITH (UPDLOCK, HOLDLOCK)
            WHERE [ProductId] = {productId}
              AND [WarehouseId] = {warehouseId}
              AND [WarehouseLocationId] = {locationId}
            """).SingleOrDefaultAsync(ct);
    }

    public static Task<WarehouseTransfer?> FindWarehouseTransferForUpdateAsync(
        this InventoryWarehouseDbContext dbContext, Guid id, CancellationToken ct)
    {
        if (!UsesSqlServerWriterLocks(dbContext.Database.ProviderName))
            return dbContext.WarehouseTransfers.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);

        return dbContext.WarehouseTransfers.FromSqlInterpolated($"""
            SELECT * FROM [WarehouseTransfers] WITH (UPDLOCK, HOLDLOCK)
            WHERE [Id] = {id}
            """).Include(x => x.Items).SingleOrDefaultAsync(ct);
    }

    internal static bool UsesSqlServerWriterLocks(string? providerName) =>
        providerName == "Microsoft.EntityFrameworkCore.SqlServer";
}
