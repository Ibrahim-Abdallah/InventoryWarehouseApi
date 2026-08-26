using System.Data;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase11MutationLockingTests
{
    [Fact]
    public void ProviderSelection_UsesHintsOnlyForSqlServer()
    {
        Assert.True(MutationLockingQueries.UsesSqlServerWriterLocks("Microsoft.EntityFrameworkCore.SqlServer"));
        Assert.False(MutationLockingQueries.UsesSqlServerWriterLocks("Microsoft.EntityFrameworkCore.Sqlite"));
        Assert.False(MutationLockingQueries.UsesSqlServerWriterLocks(null));
    }

    [Fact]
    public async Task SqlitePath_ReturnsTrackedBalanceWithoutProviderSpecificSql()
    {
        using ApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Product product = new("LOCK-SKU", "Lock product", null, now);
        Warehouse warehouse = new("LOCK-WH", "Lock warehouse", null, now);
        WarehouseLocation location = new(warehouse.Id, "LOCK-L", "Lock location", null, now);
        Guid productId = product.Id, warehouseId = warehouse.Id, locationId = location.Id;
        db.Products.Add(product);
        db.Warehouses.Add(warehouse);
        db.WarehouseLocations.Add(location);
        db.InventoryBalances.Add(new InventoryBalance(productId, warehouseId, locationId, 10m, 0m));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        InventoryBalance? balance = await db.FindInventoryBalanceForUpdateAsync(productId, warehouseId, locationId, CancellationToken.None);

        Assert.NotNull(balance);
        Assert.Equal(EntityState.Unchanged, db.Entry(balance).State);
        Assert.Equal(10m, balance.OnHandQuantity);
    }
}
