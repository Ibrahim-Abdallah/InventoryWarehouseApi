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
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase04ApiTests
{
    [Fact]
    public async Task StockIn_CreatesAndAccumulatesBalance_WithLedgerAndReferences()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var position = await CreatePosition(client, "in");

        StockMovementOperationResponse first = await Post(client, position, "stock-in", new(10m, " PurchaseReceipt ", " PR-1001 "));
        StockMovementOperationResponse second = await Post(client, position, "stock-in", new(2.5m));

        Assert.Equal((10m, 0m, 10m), (first.OnHandQuantity, first.ReservedQuantity, first.AvailableQuantity));
        Assert.Equal((12.5m, 0m, 12.5m), (second.OnHandQuantity, second.ReservedQuantity, second.AvailableQuantity));
        Assert.Equal("PurchaseReceipt", first.ReferenceType);
        Assert.Equal("PR-1001", first.ReferenceId);
        Assert.Null(second.ReferenceType);
        PagedResult<StockMovementResponse> history = (await client.GetFromJsonAsync<PagedResult<StockMovementResponse>>(Url(position, "movements")))!;
        Assert.Equal(2, history.TotalCount);
        Assert.Equal(second.MovementId, history.Items[0].Id);
        Assert.All(history.Items, x => Assert.Equal(StockMovementType.StockIn, x.MovementType));
    }

    [Fact]
    public async Task StockOut_UsesAvailableStock_PreservesReserved_AndFailureIsAtomic()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var position = await CreatePosition(client, "out");
        await SeedBalance(factory, position, 10m, 4m);

        StockMovementOperationResponse issued = await Post(client, position, "stock-out", new(6m, "Order", "O-1"));
        Assert.Equal((4m, 4m, 0m), (issued.OnHandQuantity, issued.ReservedQuantity, issued.AvailableQuantity));
        HttpResponseMessage failed = await client.PostAsJsonAsync(Url(position, "stock-out"), new StockMovementRequest(1m));
        Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode);
        Assert.Contains("Insufficient available stock", await failed.Content.ReadAsStringAsync());

        LocationInventoryResponse balance = (await client.GetFromJsonAsync<LocationInventoryResponse>(Url(position, null)))!;
        PagedResult<StockMovementResponse> history = (await client.GetFromJsonAsync<PagedResult<StockMovementResponse>>(Url(position, "movements")))!;
        Assert.Equal((4m, 4m, 0m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
        Assert.Single(history.Items);
        Assert.Equal(StockMovementType.StockOut, history.Items[0].MovementType);
    }

    [Fact]
    public async Task StockOut_WithoutBalance_ConflictsWithoutCreatingBalanceOrMovement()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var position = await CreatePosition(client, "empty");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Url(position, "stock-out"), new StockMovementRequest(1m))).StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        Assert.False(await db.InventoryBalances.AnyAsync());
        Assert.False(await db.StockMovements.AnyAsync());
    }

    [Fact]
    public async Task Commands_ValidateResourcesPositionAndRequest()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var position = await CreatePosition(client, "valid");
        WarehouseResponse otherWarehouse = await CreateWarehouse(client, "other");
        WarehouseLocationResponse otherLocation = await CreateLocation(client, otherWarehouse.Id, "OTHER");

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Url((Guid.NewGuid(), position.WarehouseId, position.LocationId), "stock-in"), new StockMovementRequest(1m))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Url((position.ProductId, Guid.NewGuid(), position.LocationId), "stock-in"), new StockMovementRequest(1m))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Url((position.ProductId, position.WarehouseId, Guid.NewGuid()), "stock-in"), new StockMovementRequest(1m))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Url((position.ProductId, position.WarehouseId, otherLocation.Id), "stock-in"), new StockMovementRequest(1m))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Url(position, "stock-in"), new StockMovementRequest(0m, "one-sided", null))).StatusCode);
    }

    [Fact]
    public async Task History_IsPositionScopedNewestFirstAndPaged()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var first = await CreatePosition(client, "history-a");
        var second = await CreatePosition(client, "history-b");
        StockMovementOperationResponse oldest = await Post(client, first, "stock-in", new(5m, "Receipt", "R-1"));
        StockMovementOperationResponse newest = await Post(client, first, "stock-out", new(2m, "Order", "O-2"));
        await Post(client, second, "stock-in", new(99m));

        PagedResult<StockMovementResponse> page1 = (await client.GetFromJsonAsync<PagedResult<StockMovementResponse>>(Url(first, "movements") + "?pageNumber=1&pageSize=1"))!;
        PagedResult<StockMovementResponse> page2 = (await client.GetFromJsonAsync<PagedResult<StockMovementResponse>>(Url(first, "movements") + "?pageNumber=2&pageSize=1"))!;
        Assert.Equal(2, page1.TotalCount);
        Assert.Equal(newest.MovementId, Assert.Single(page1.Items).Id);
        Assert.Equal(oldest.MovementId, Assert.Single(page2.Items).Id);
        Assert.Equal("Receipt", page2.Items[0].ReferenceType);
    }

    [Fact]
    public async Task DatabaseConstraintsMappingAndDeleteIntegrity_AreEnforced()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var position = await CreatePosition(client, "db");
        await Post(client, position, "stock-in", new(1m));
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/products/{position.ProductId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/warehouses/{position.WarehouseId}/locations/{position.LocationId}")).StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        var quantity = db.Model.FindEntityType(typeof(StockMovement))!.FindProperty(nameof(StockMovement.Quantity))!;
        Assert.Equal(18, quantity.GetPrecision());
        Assert.Equal(3, quantity.GetScale());
        Assert.Null(db.Model.FindEntityType(typeof(InventoryBalance))!.FindProperty(nameof(InventoryBalance.AvailableQuantity)));

        await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO StockMovements (Id,ProductId,WarehouseId,WarehouseLocationId,MovementType,Quantity,OccurredAtUtc) VALUES ({Guid.NewGuid()},{position.ProductId},{position.WarehouseId},{position.LocationId},{1},{0m},{DateTimeOffset.UtcNow})");
        });
        await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO StockMovements (Id,ProductId,WarehouseId,WarehouseLocationId,MovementType,Quantity,OccurredAtUtc) VALUES ({Guid.NewGuid()},{position.ProductId},{position.WarehouseId},{position.LocationId},{99},{1m},{DateTimeOffset.UtcNow})");
        });
        await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO StockMovements (Id,ProductId,WarehouseId,WarehouseLocationId,MovementType,Quantity,OccurredAtUtc) VALUES ({Guid.NewGuid()},{position.ProductId},{position.WarehouseId},{Guid.NewGuid()},{1},{1m},{DateTimeOffset.UtcNow})");
        });
    }

    private static async Task<(Guid ProductId, Guid WarehouseId, Guid LocationId)> CreatePosition(HttpClient client, string suffix)
    {
        ProductResponse product = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest("sku-" + suffix, suffix, null))).Content.ReadFromJsonAsync<ProductResponse>())!;
        WarehouseResponse warehouse = await CreateWarehouse(client, "wh-" + suffix);
        WarehouseLocationResponse location = await CreateLocation(client, warehouse.Id, "L-" + suffix);
        return (product.Id, warehouse.Id, location.Id);
    }
    private static async Task<WarehouseResponse> CreateWarehouse(HttpClient client, string code) =>
        (await (await client.PostAsJsonAsync("/api/warehouses", new CreateWarehouseRequest(code, code, null))).Content.ReadFromJsonAsync<WarehouseResponse>())!;
    private static async Task<WarehouseLocationResponse> CreateLocation(HttpClient client, Guid warehouseId, string code) =>
        (await (await client.PostAsJsonAsync($"/api/warehouses/{warehouseId}/locations", new CreateWarehouseLocationRequest(code, code, null))).Content.ReadFromJsonAsync<WarehouseLocationResponse>())!;
    private static string Url((Guid ProductId, Guid WarehouseId, Guid LocationId) p, string? suffix) =>
        $"/api/inventory/products/{p.ProductId}/warehouses/{p.WarehouseId}/locations/{p.LocationId}" + (suffix is null ? "" : "/" + suffix);
    private static async Task<StockMovementOperationResponse> Post(HttpClient client, (Guid ProductId, Guid WarehouseId, Guid LocationId) p, string operation, StockMovementRequest request) =>
        (await (await client.PostAsJsonAsync(Url(p, operation), request)).Content.ReadFromJsonAsync<StockMovementOperationResponse>())!;
    private static async Task SeedBalance(ApiFactory factory, (Guid ProductId, Guid WarehouseId, Guid LocationId) p, decimal onHand, decimal reserved)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        db.InventoryBalances.Add(new InventoryBalance(p.ProductId, p.WarehouseId, p.LocationId, onHand, reserved));
        await db.SaveChangesAsync();
    }
}
