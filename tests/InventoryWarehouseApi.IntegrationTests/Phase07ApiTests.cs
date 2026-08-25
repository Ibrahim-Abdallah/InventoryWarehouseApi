using System.Net;
using System.Net.Http.Json;
using InventoryWarehouseApi.Application.InventoryReservations;
using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.WarehouseLocations;
using InventoryWarehouseApi.Application.Warehouses;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase07ApiTests
{
    [Fact]
    public async Task CreateReleaseAndFulfill_ApplyBalancesAndLedgerExactly()
    {
        using ApiFactory factory = new(); using HttpClient client = factory.CreateClient();
        var first = await Position(client, "release"); await Seed(factory, first, 10m);
        InventoryReservationResponse released = await Create(client, first, 4m, " Order ", " ORD-1 ");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/inventory-reservations/{released.Id}/release", null)).StatusCode);
        var second = await Position(client, "fulfill"); await Seed(factory, second, 10m);
        InventoryReservationResponse active = await Create(client, second, 4m);
        HttpResponseMessage fulfilledResponse = await client.PostAsync($"/api/inventory-reservations/{active.Id}/fulfill", null);
        InventoryReservationResponse fulfilled = (await fulfilledResponse.Content.ReadFromJsonAsync<InventoryReservationResponse>())!;
        Assert.Equal(InventoryReservationStatus.Fulfilled, fulfilled.Status);
        using IServiceScope scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        InventoryBalance b1 = await db.InventoryBalances.SingleAsync(x => x.ProductId == first.ProductId);
        InventoryBalance b2 = await db.InventoryBalances.SingleAsync(x => x.ProductId == second.ProductId);
        Assert.Equal((10m, 0m, 10m), (b1.OnHandQuantity, b1.ReservedQuantity, b1.AvailableQuantity));
        Assert.Equal((6m, 0m, 6m), (b2.OnHandQuantity, b2.ReservedQuantity, b2.AvailableQuantity));
        StockMovement movement = await db.StockMovements.SingleAsync();
        Assert.Equal((StockMovementType.StockOut, "InventoryReservation", active.Id.ToString("D")), (movement.MovementType, movement.ReferenceType, movement.ReferenceId));
        Assert.Equal(fulfilled.FulfilledAtUtc, movement.OccurredAtUtc); Assert.Equal(fulfilled.FulfillmentMovementId, movement.Id);
    }

    [Fact]
    public async Task Create_ReservesOnly_TrimsReferences_AndCanBeReadAndListed()
    {
        using ApiFactory factory = new(); using HttpClient client = factory.CreateClient();
        var p = await Position(client, "create"); await Seed(factory, p, 10m);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory-reservations", Request(p, 4m, " Order ", " R-1 "));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        InventoryReservationResponse created = (await response.Content.ReadFromJsonAsync<InventoryReservationResponse>())!;
        Assert.Equal(("Order", "R-1"), (created.ReferenceType, created.ReferenceId));
        Assert.Equal(created.Id, (await client.GetFromJsonAsync<InventoryReservationResponse>($"/api/inventory-reservations/{created.Id}"))!.Id);
        Assert.Single((await client.GetFromJsonAsync<InventoryWarehouseApi.Application.Common.PagedResult<InventoryReservationResponse>>("/api/inventory-reservations"))!.Items);
        using IServiceScope scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        InventoryBalance balance = await db.InventoryBalances.SingleAsync();
        Assert.Equal((10m, 4m, 6m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
        Assert.False(await db.StockMovements.AnyAsync());
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(1.0001)]
    public async Task Create_InvalidQuantity_ReturnsBadRequest(decimal quantity)
    {
        using ApiFactory factory = new(); using HttpClient client = factory.CreateClient(); var p = await Position(client, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/inventory-reservations", Request(p, quantity))).StatusCode);
    }

    [Fact]
    public async Task Create_MissingBalanceOrInsufficientAvailability_ReturnsConflictWithoutMutation()
    {
        using ApiFactory factory = new(); using HttpClient client = factory.CreateClient(); var p = await Position(client, "conflict");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/inventory-reservations", Request(p, 1m))).StatusCode);
        await Seed(factory, p, 10m, 4m);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/inventory-reservations", Request(p, 7m))).StatusCode);
        using IServiceScope scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        Assert.False(await db.InventoryReservations.AnyAsync()); Assert.Equal(4m, (await db.InventoryBalances.SingleAsync()).ReservedQuantity);
    }

    [Fact]
    public async Task TerminalTransitions_ReturnConflictAndNeverDuplicateMovement()
    {
        using ApiFactory factory = new(); using HttpClient client = factory.CreateClient(); var p = await Position(client, "repeat"); await Seed(factory, p, 10m);
        InventoryReservationResponse reservation = await Create(client, p, 4m);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/inventory-reservations/{reservation.Id}/fulfill", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/inventory-reservations/{reservation.Id}/fulfill", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/inventory-reservations/{reservation.Id}/release", null)).StatusCode);
        using IServiceScope scope = factory.Services.CreateScope(); Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>().StockMovements.CountAsync());
    }

    [Fact]
    public async Task ActiveReservation_RestrictsNormalStockOutToAvailableQuantity()
    {
        using ApiFactory factory = new(); using HttpClient client = factory.CreateClient(); var p = await Position(client, "stockout"); await Seed(factory, p, 10m);
        await Create(client, p, 4m);
        string endpoint = $"/api/inventory/products/{p.ProductId}/warehouses/{p.WarehouseId}/locations/{p.LocationId}/stock-out";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(endpoint, new StockMovementRequest(7m))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(endpoint, new StockMovementRequest(6m))).StatusCode);
        using IServiceScope scope = factory.Services.CreateScope(); InventoryBalance balance = await scope.ServiceProvider
            .GetRequiredService<InventoryWarehouseDbContext>().InventoryBalances.SingleAsync();
        Assert.Equal((4m, 4m, 0m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
    }

    private static CreateInventoryReservationRequest Request((Guid ProductId, Guid WarehouseId, Guid LocationId) p, decimal q, string? rt = null, string? ri = null) => new(p.ProductId, p.WarehouseId, p.LocationId, q, rt, ri);
    private static async Task<InventoryReservationResponse> Create(HttpClient c, (Guid ProductId, Guid WarehouseId, Guid LocationId) p, decimal q, string? rt = null, string? ri = null) =>
        (await (await c.PostAsJsonAsync("/api/inventory-reservations", Request(p, q, rt, ri))).Content.ReadFromJsonAsync<InventoryReservationResponse>())!;
    private static async Task<(Guid ProductId, Guid WarehouseId, Guid LocationId)> Position(HttpClient c, string s)
    {
        ProductResponse product = (await (await c.PostAsJsonAsync("/api/products", new CreateProductRequest("sku-" + s, s, null))).Content.ReadFromJsonAsync<ProductResponse>())!;
        WarehouseResponse warehouse = (await (await c.PostAsJsonAsync("/api/warehouses", new CreateWarehouseRequest("wh-" + s, s, null))).Content.ReadFromJsonAsync<WarehouseResponse>())!;
        WarehouseLocationResponse location = (await (await c.PostAsJsonAsync($"/api/warehouses/{warehouse.Id}/locations", new CreateWarehouseLocationRequest("L-" + s, s, null))).Content.ReadFromJsonAsync<WarehouseLocationResponse>())!;
        return (product.Id, warehouse.Id, location.Id);
    }
    private static async Task Seed(ApiFactory f, (Guid ProductId, Guid WarehouseId, Guid LocationId) p, decimal onHand, decimal reserved = 0)
    { using IServiceScope s = f.Services.CreateScope(); var db = s.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>(); db.InventoryBalances.Add(new(p.ProductId, p.WarehouseId, p.LocationId, onHand, reserved)); await db.SaveChangesAsync(); }
}
