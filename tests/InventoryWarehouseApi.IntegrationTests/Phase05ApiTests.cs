using System.Net;
using System.Net.Http.Json;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.WarehouseLocations;
using InventoryWarehouseApi.Application.Warehouses;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase05ApiTests
{
    [Fact]
    public async Task Increase_CreatesAndUpdatesBalance_AuditAndLinkedLedger()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var p = await CreatePosition(client, "inc");
        InventoryAdjustmentOperationResponse first = await Post(client, p, "increase", new(5m, " Count correction ", " manager "));
        InventoryAdjustmentOperationResponse second = await Post(client, p, "increase", new(2m, "Receipt correction", "manager"));

        Assert.Equal((7m, 0m, 7m), (second.OnHandQuantity, second.ReservedQuantity, second.AvailableQuantity));
        Assert.Equal(("Count correction", "manager"), (first.Reason, first.AdjustedBy));
        Assert.Equal(StockMovementType.AdjustmentIncrease, first.StockMovementType);
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        Assert.Equal(2, await db.InventoryAdjustments.CountAsync());
        Assert.Equal(2, await db.StockMovements.CountAsync());
        StockMovement movement = await db.StockMovements.SingleAsync(x => x.Id == first.StockMovementId);
        Assert.Equal("InventoryAdjustment", movement.ReferenceType);
        Assert.Equal(first.AdjustmentId.ToString("D"), movement.ReferenceId);
    }

    [Fact]
    public async Task Decrease_UsesAvailable_PreservesReserved_AndFailuresAreAtomic()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var p = await CreatePosition(client, "dec");
        await SeedBalance(factory, p, 10m, 4m);
        InventoryAdjustmentOperationResponse result = await Post(client, p, "decrease", new(6m, "Physical count", "auditor"));
        Assert.Equal((4m, 4m, 0m), (result.OnHandQuantity, result.ReservedQuantity, result.AvailableQuantity));
        Assert.Equal(StockMovementType.AdjustmentDecrease, result.StockMovementType);

        HttpResponseMessage failed = await client.PostAsJsonAsync(Url(p, "adjustments/decrease"), new InventoryAdjustmentRequest(1m, "bad", "auditor"));
        Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        Assert.Single(await db.InventoryAdjustments.ToListAsync());
        Assert.Single(await db.StockMovements.ToListAsync());
        InventoryBalance balance = await db.InventoryBalances.SingleAsync();
        Assert.Equal((4m, 4m), (balance.OnHandQuantity, balance.ReservedQuantity));
    }

    [Fact]
    public async Task DecreaseWithoutBalance_ConflictsWithoutPersistence()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var p = await CreatePosition(client, "empty-adj");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Url(p, "adjustments/decrease"), new InventoryAdjustmentRequest(1m, "count", "user"))).StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        Assert.False(await db.InventoryBalances.AnyAsync());
        Assert.False(await db.InventoryAdjustments.AnyAsync());
        Assert.False(await db.StockMovements.AnyAsync());
    }

    [Fact]
    public async Task Commands_ValidateResourcesAndPayload()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var p = await CreatePosition(client, "validation-adj");
        InventoryAdjustmentRequest valid = new(1m, "reason", "user");
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Url((Guid.NewGuid(), p.WarehouseId, p.LocationId), "adjustments/increase"), valid)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Url((p.ProductId, Guid.NewGuid(), p.LocationId), "adjustments/increase"), valid)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Url((p.ProductId, p.WarehouseId, Guid.NewGuid()), "adjustments/increase"), valid)).StatusCode);
        InventoryAdjustmentRequest[] invalid = [new(0m, "reason", "user"), new(-1m, "reason", "user"),
            new(1.0001m, "reason", "user"), new(1m, " ", "user"), new(1m, new string('r', 501), "user"),
            new(1m, "reason", " "), new(1m, "reason", new string('u', 129))];
        foreach (InventoryAdjustmentRequest request in invalid)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Url(p, "adjustments/increase"), request)).StatusCode);
    }

    [Fact]
    public async Task History_IsPositionScoped_NewestFirst_Paged_AndMovementHistoryIncludesAdjustments()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var first = await CreatePosition(client, "hist-adj-a");
        var second = await CreatePosition(client, "hist-adj-b");
        InventoryAdjustmentOperationResponse oldest = await Post(client, first, "increase", new(5m, "first", "one"));
        InventoryAdjustmentOperationResponse newest = await Post(client, first, "decrease", new(2m, "second", "two"));
        await Post(client, second, "increase", new(9m, "other", "three"));
        PagedResult<InventoryAdjustmentResponse> page1 = (await client.GetFromJsonAsync<PagedResult<InventoryAdjustmentResponse>>(Url(first, "adjustments?pageNumber=1&pageSize=1")))!;
        PagedResult<InventoryAdjustmentResponse> page2 = (await client.GetFromJsonAsync<PagedResult<InventoryAdjustmentResponse>>(Url(first, "adjustments?pageNumber=2&pageSize=1")))!;
        Assert.Equal(2, page1.TotalCount);
        Assert.Equal(newest.AdjustmentId, Assert.Single(page1.Items).Id);
        Assert.Equal(oldest.AdjustmentId, Assert.Single(page2.Items).Id);
        PagedResult<StockMovementResponse> movements = (await client.GetFromJsonAsync<PagedResult<StockMovementResponse>>(Url(first, "movements")))!;
        Assert.Contains(movements.Items, x => x.MovementType == StockMovementType.AdjustmentIncrease && x.ReferenceId == oldest.AdjustmentId.ToString("D"));
        Assert.Contains(movements.Items, x => x.MovementType == StockMovementType.AdjustmentDecrease && x.ReferenceId == newest.AdjustmentId.ToString("D"));
    }

    [Fact]
    public async Task DatabaseConstraintsRelationshipsAndDeleteIntegrity_AreEnforced()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var p = await CreatePosition(client, "db-adj");
        InventoryAdjustmentOperationResponse response = await Post(client, p, "increase", new(1m, "reason", "user"));
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/products/{p.ProductId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/warehouses/{p.WarehouseId}/locations/{p.LocationId}")).StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        var quantity = db.Model.FindEntityType(typeof(InventoryAdjustment))!.FindProperty(nameof(InventoryAdjustment.Quantity))!;
        Assert.Equal((18, 3), (quantity.GetPrecision(), quantity.GetScale()));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO InventoryAdjustments (Id,ProductId,WarehouseId,WarehouseLocationId,AdjustmentType,Quantity,Reason,AdjustedBy,StockMovementId,AdjustedAtUtc) VALUES ({Guid.NewGuid()},{p.ProductId},{p.WarehouseId},{p.LocationId},{1},{0m},{"r"},{"u"},{response.StockMovementId},{DateTimeOffset.UtcNow})"));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO InventoryAdjustments (Id,ProductId,WarehouseId,WarehouseLocationId,AdjustmentType,Quantity,Reason,AdjustedBy,StockMovementId,AdjustedAtUtc) VALUES ({Guid.NewGuid()},{p.ProductId},{p.WarehouseId},{p.LocationId},{99},{1m},{"r"},{"u"},{response.StockMovementId},{DateTimeOffset.UtcNow})"));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO InventoryAdjustments (Id,ProductId,WarehouseId,WarehouseLocationId,AdjustmentType,Quantity,Reason,AdjustedBy,StockMovementId,AdjustedAtUtc) VALUES ({Guid.NewGuid()},{p.ProductId},{p.WarehouseId},{p.LocationId},{1},{1m},{"r"},{"u"},{response.StockMovementId},{DateTimeOffset.UtcNow})"));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO InventoryAdjustments (Id,ProductId,WarehouseId,WarehouseLocationId,AdjustmentType,Quantity,Reason,AdjustedBy,StockMovementId,AdjustedAtUtc) VALUES ({Guid.NewGuid()},{p.ProductId},{p.WarehouseId},{Guid.NewGuid()},{1},{1m},{"r"},{"u"},{Guid.NewGuid()},{DateTimeOffset.UtcNow})"));
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO StockMovements (Id,ProductId,WarehouseId,WarehouseLocationId,MovementType,Quantity,OccurredAtUtc) VALUES ({Guid.NewGuid()},{p.ProductId},{p.WarehouseId},{p.LocationId},{3},{1m},{DateTimeOffset.UtcNow})");
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO StockMovements (Id,ProductId,WarehouseId,WarehouseLocationId,MovementType,Quantity,OccurredAtUtc) VALUES ({Guid.NewGuid()},{p.ProductId},{p.WarehouseId},{p.LocationId},{99},{1m},{DateTimeOffset.UtcNow})"));
    }

    private static async Task<(Guid ProductId, Guid WarehouseId, Guid LocationId)> CreatePosition(HttpClient client, string suffix)
    {
        ProductResponse product = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest("sku-" + suffix, suffix, null))).Content.ReadFromJsonAsync<ProductResponse>())!;
        WarehouseResponse warehouse = (await (await client.PostAsJsonAsync("/api/warehouses", new CreateWarehouseRequest("wh-" + suffix, suffix, null))).Content.ReadFromJsonAsync<WarehouseResponse>())!;
        WarehouseLocationResponse location = (await (await client.PostAsJsonAsync($"/api/warehouses/{warehouse.Id}/locations", new CreateWarehouseLocationRequest("L-" + suffix, suffix, null))).Content.ReadFromJsonAsync<WarehouseLocationResponse>())!;
        return (product.Id, warehouse.Id, location.Id);
    }
    private static string Url((Guid ProductId, Guid WarehouseId, Guid LocationId) p, string suffix) =>
        $"/api/inventory/products/{p.ProductId}/warehouses/{p.WarehouseId}/locations/{p.LocationId}/{suffix}";
    private static async Task<InventoryAdjustmentOperationResponse> Post(HttpClient client,
        (Guid ProductId, Guid WarehouseId, Guid LocationId) p, string operation, InventoryAdjustmentRequest request) =>
        (await (await client.PostAsJsonAsync(Url(p, "adjustments/" + operation), request)).Content.ReadFromJsonAsync<InventoryAdjustmentOperationResponse>())!;
    private static async Task SeedBalance(ApiFactory factory, (Guid ProductId, Guid WarehouseId, Guid LocationId) p,
        decimal onHand, decimal reserved)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        db.InventoryBalances.Add(new(p.ProductId, p.WarehouseId, p.LocationId, onHand, reserved));
        await db.SaveChangesAsync();
    }
}
