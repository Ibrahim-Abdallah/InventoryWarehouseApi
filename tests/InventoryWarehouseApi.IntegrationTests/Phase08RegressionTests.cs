using System.Net.Http.Json;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Application.InventoryReservations;
using InventoryWarehouseApi.Application.LowStock;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.WarehouseLocations;
using InventoryWarehouseApi.Application.Warehouses;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase08RegressionTests
{
    [Fact]
    public async Task AlertHistoryEndpoint_FiltersPagesAndOrdersBeforeResponseProjection()
    {
        using ApiFactory factory = new(); using HttpClient client = factory.CreateClient();
        Position first = await CreatePosition(client, "alerts-first");
        Position second = await CreatePosition(client, "alerts-second");
        LowStockThresholdResponse firstThreshold = await Threshold(client, first, 5m, true);
        LowStockThresholdResponse secondThreshold = await Threshold(client, second, 5m, true);
        DateTimeOffset older = new(2026, 8, 25, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset newer = older.AddMinutes(1);
        Guid lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Guid newestId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
            LowStockAlert firstResolved = new(lowerId, firstThreshold.Id, 5m, 4m, older); firstResolved.Resolve(8m, older.AddSeconds(1));
            LowStockAlert secondResolved = new(higherId, secondThreshold.Id, 5m, 3m, older); secondResolved.Resolve(7m, older.AddSeconds(1));
            LowStockAlert active = new(newestId, firstThreshold.Id, 5m, 2m, newer);
            db.LowStockAlerts.AddRange(firstResolved, secondResolved, active); await db.SaveChangesAsync();
        }

        PagedResult<LowStockAlertResponse> all = await GetAlerts(client, "pageNumber=1&pageSize=20");
        Assert.Equal((1, 20, 3, 1), (all.PageNumber, all.PageSize, all.TotalCount, all.TotalPages));
        Assert.Equal([newestId, higherId, lowerId], all.Items.Select(x => x.Id));

        PagedResult<LowStockAlertResponse> activeOnly = await GetAlerts(client, "isActive=true&pageNumber=1&pageSize=20");
        Assert.Single(activeOnly.Items); Assert.All(activeOnly.Items, x => Assert.True(x.IsActive));
        PagedResult<LowStockAlertResponse> resolvedOnly = await GetAlerts(client, "isActive=false&pageNumber=1&pageSize=20");
        Assert.Equal(2, resolvedOnly.TotalCount); Assert.All(resolvedOnly.Items, x => Assert.False(x.IsActive));
        Assert.Equal([higherId, lowerId], resolvedOnly.Items.Select(x => x.Id));

        PagedResult<LowStockAlertResponse> product = await GetAlerts(client, $"productId={first.ProductId}&pageNumber=1&pageSize=1");
        Assert.Equal((2, 1, 2), (product.TotalCount, product.Items.Count, product.TotalPages)); Assert.All(product.Items, x => Assert.Equal(first.ProductId, x.ProductId));
        PagedResult<LowStockAlertResponse> warehouse = await GetAlerts(client, $"warehouseId={second.WarehouseId}&pageNumber=1&pageSize=20");
        Assert.Single(warehouse.Items); Assert.Equal(second.WarehouseId, warehouse.Items[0].WarehouseId);
    }

    [Fact]
    public async Task Reservation_MakesPositionLow_TriggersAlert_AndReleaseResolvesWithoutMovement()
    {
        using ApiFactory factory = new(); using HttpClient client = factory.CreateClient();
        Position p = await CreatePosition(client, "reservation-flow");
        await Stock(client, p, "stock-in", 10m);
        LowStockThresholdResponse threshold = await Threshold(client, p, 6m, true);
        Assert.Equal((10m, 0m, 10m), await Quantities(factory, p));
        Assert.Empty((await Low(client, p)).Items);

        InventoryReservationResponse reservation = (await (await client.PostAsJsonAsync("/api/inventory-reservations",
            new CreateInventoryReservationRequest(p.ProductId, p.WarehouseId, p.LocationId, 4m, null, null)))
            .Content.ReadFromJsonAsync<InventoryReservationResponse>())!;
        Assert.Equal((10m, 4m, 6m), await Quantities(factory, p));
        Assert.Single((await Low(client, p)).Items);

        DateTimeOffset firstScan = DateTimeOffset.UtcNow;
        await RunMonitor(factory, firstScan);
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
            LowStockAlert alert = Assert.Single(await db.LowStockAlerts.Where(x => x.LowStockThresholdId == threshold.Id).ToListAsync());
            Assert.Null(alert.ResolvedAtUtc); Assert.Equal((6m, 6m), (alert.ThresholdQuantity, alert.AvailableQuantity));
            Assert.Single(await db.StockMovements.ToListAsync());
        }

        await client.PostAsync($"/api/inventory-reservations/{reservation.Id}/release", null);
        Assert.Equal((10m, 0m, 10m), await Quantities(factory, p));
        Assert.Empty((await Low(client, p)).Items);
        await RunMonitor(factory, firstScan.AddSeconds(1));
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
            LowStockAlert alert = Assert.Single(await db.LowStockAlerts.Where(x => x.LowStockThresholdId == threshold.Id).ToListAsync());
            Assert.NotNull(alert.ResolvedAtUtc); Assert.Empty(await db.LowStockAlerts.Where(x => x.LowStockThresholdId == threshold.Id && x.ResolvedAtUtc == null).ToListAsync());
            Assert.Single(await db.StockMovements.ToListAsync());
        }
    }

    [Fact]
    public async Task StockOut_TriggersOneAlert_AndRecoveryStockInResolvesItWithoutMonitorMovements()
    {
        using ApiFactory factory = new(); using HttpClient client = factory.CreateClient();
        Position p = await CreatePosition(client, "movement-flow"); await Stock(client, p, "stock-in", 10m);
        LowStockThresholdResponse threshold = await Threshold(client, p, 5m, true);
        Assert.Empty((await Low(client, p)).Items);
        await Stock(client, p, "stock-out", 6m);
        Assert.Equal((4m, 0m, 4m), await Quantities(factory, p)); Assert.Single((await Low(client, p)).Items);

        DateTimeOffset firstScan = DateTimeOffset.UtcNow; await RunMonitor(factory, firstScan);
        await RunMonitor(factory, firstScan.AddSeconds(1));
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
            LowStockAlert alert = Assert.Single(await db.LowStockAlerts.Where(x => x.LowStockThresholdId == threshold.Id).ToListAsync());
            Assert.Null(alert.ResolvedAtUtc); Assert.Equal((5m, 4m), (alert.ThresholdQuantity, alert.AvailableQuantity)); Assert.Equal(firstScan.AddSeconds(1), alert.LastObservedAtUtc);
            Assert.Equal(2, await db.StockMovements.CountAsync());
        }

        await Stock(client, p, "stock-in", 3m);
        Assert.Equal((7m, 0m, 7m), await Quantities(factory, p)); Assert.Empty((await Low(client, p)).Items);
        await RunMonitor(factory, firstScan.AddSeconds(2));
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
            LowStockAlert alert = Assert.Single(await db.LowStockAlerts.Where(x => x.LowStockThresholdId == threshold.Id).ToListAsync());
            Assert.NotNull(alert.ResolvedAtUtc); Assert.Equal(7m, alert.AvailableQuantity); Assert.Equal(3, await db.StockMovements.CountAsync());
        }
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(10, false)]
    public async Task ThresholdChange_IsImmediateForQuery_AndEventualForAlert(decimal newQuantity, bool enabled)
    {
        using ApiFactory factory = new(); using HttpClient client = factory.CreateClient();
        Position p = await CreatePosition(client, $"threshold-{newQuantity}-{enabled}"); await Stock(client, p, "stock-in", 8m);
        LowStockThresholdResponse threshold = await Threshold(client, p, 10m, true);
        DateTimeOffset firstScan = DateTimeOffset.UtcNow; await RunMonitor(factory, firstScan);
        await Threshold(client, p, newQuantity, enabled);
        Assert.Empty((await Low(client, p)).Items);
        using (IServiceScope before = factory.Services.CreateScope())
            Assert.Single(await before.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>().LowStockAlerts.Where(x => x.LowStockThresholdId == threshold.Id && x.ResolvedAtUtc == null).ToListAsync());
        await RunMonitor(factory, firstScan.AddSeconds(1));
        using IServiceScope after = factory.Services.CreateScope(); InventoryWarehouseDbContext db = after.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        Assert.Empty(await db.LowStockAlerts.Where(x => x.LowStockThresholdId == threshold.Id && x.ResolvedAtUtc == null).ToListAsync());
        Assert.Single(await db.LowStockAlerts.Where(x => x.LowStockThresholdId == threshold.Id && x.ResolvedAtUtc != null).ToListAsync());
    }

    private static async Task RunMonitor(ApiFactory factory, DateTimeOffset at)
    { using IServiceScope scope = factory.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<ILowStockMonitoringService>().RunAsync(at, CancellationToken.None); }
    private static async Task<PagedResult<LowStockResponse>> Low(HttpClient client, Position p) => (await client.GetFromJsonAsync<PagedResult<LowStockResponse>>($"/api/low-stock?productId={p.ProductId}&warehouseId={p.WarehouseId}"))!;
    private static async Task<PagedResult<LowStockAlertResponse>> GetAlerts(HttpClient client, string query)
    { HttpResponseMessage response = await client.GetAsync("/api/low-stock-alerts?" + query); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<PagedResult<LowStockAlertResponse>>())!; }
    private static async Task Stock(HttpClient client, Position p, string operation, decimal quantity) => (await client.PostAsJsonAsync($"/api/inventory/products/{p.ProductId}/warehouses/{p.WarehouseId}/locations/{p.LocationId}/{operation}", new StockMovementRequest(quantity))).EnsureSuccessStatusCode();
    private static async Task<LowStockThresholdResponse> Threshold(HttpClient client, Position p, decimal quantity, bool enabled) => (await (await client.PutAsJsonAsync($"/api/low-stock-thresholds/products/{p.ProductId}/warehouses/{p.WarehouseId}/locations/{p.LocationId}", new UpsertLowStockThresholdRequest(quantity, enabled))).Content.ReadFromJsonAsync<LowStockThresholdResponse>())!;
    private static async Task<(decimal OnHand, decimal Reserved, decimal Available)> Quantities(ApiFactory factory, Position p)
    { using IServiceScope scope = factory.Services.CreateScope(); InventoryBalance b = await scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>().InventoryBalances.AsNoTracking().SingleAsync(x => x.ProductId == p.ProductId); return (b.OnHandQuantity, b.ReservedQuantity, b.AvailableQuantity); }
    private static async Task<Position> CreatePosition(HttpClient client, string suffix)
    {
        ProductResponse p = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest("sku-" + suffix, suffix, null))).Content.ReadFromJsonAsync<ProductResponse>())!;
        WarehouseResponse w = (await (await client.PostAsJsonAsync("/api/warehouses", new CreateWarehouseRequest("wh-" + suffix, suffix, null))).Content.ReadFromJsonAsync<WarehouseResponse>())!;
        WarehouseLocationResponse l = (await (await client.PostAsJsonAsync($"/api/warehouses/{w.Id}/locations", new CreateWarehouseLocationRequest("l-" + suffix, suffix, null))).Content.ReadFromJsonAsync<WarehouseLocationResponse>())!;
        return new(p.Id, w.Id, l.Id);
    }
    private sealed record Position(Guid ProductId, Guid WarehouseId, Guid LocationId);
}
