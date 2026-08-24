using System.Net;
using System.Net.Http.Json;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.WarehouseLocations;
using InventoryWarehouseApi.Application.Warehouses;
using InventoryWarehouseApi.Application.WarehouseTransfers;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase06ApiTests
{
    [Fact]
    public async Task Create_IsPendingAndDoesNotMutateInventoryOrLedger()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var p = await CreatePosition(client, "create");
        await Seed(factory, p.ProductId, p.SourceWarehouseId, p.SourceLocationId, 10m, 4m);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/warehouse-transfers", Request(p, [(p.ProductId, 6m)]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        WarehouseTransferResponse transfer = (await response.Content.ReadFromJsonAsync<WarehouseTransferResponse>())!;
        Assert.Equal(WarehouseTransferStatus.Pending, transfer.Status);
        Assert.Null(transfer.CompletedAtUtc);
        Assert.Single(transfer.Items);
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        InventoryBalance balance = await db.InventoryBalances.SingleAsync();
        Assert.Equal((10m, 4m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.False(await db.StockMovements.AnyAsync());
    }

    [Fact]
    public async Task Complete_ConservesStock_PreservesReservations_AndLinksUnifiedLedger()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var p = await CreatePosition(client, "complete");
        await Seed(factory, p.ProductId, p.SourceWarehouseId, p.SourceLocationId, 10m, 4m);
        await Seed(factory, p.ProductId, p.DestinationWarehouseId, p.DestinationLocationId, 3m, 1m);
        WarehouseTransferResponse pending = await Create(client, Request(p, [(p.ProductId, 6m)]));
        WarehouseTransferResponse completed = (await (await client.PostAsync($"/api/warehouse-transfers/{pending.Id}/complete", null))
            .Content.ReadFromJsonAsync<WarehouseTransferResponse>())!;
        Assert.Equal(WarehouseTransferStatus.Completed, completed.Status);
        WarehouseTransferItemResponse item = Assert.Single(completed.Items);
        Assert.NotNull(item.TransferOutMovementId);
        Assert.NotNull(item.TransferInMovementId);
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        InventoryBalance source = await db.InventoryBalances.SingleAsync(x => x.WarehouseId == p.SourceWarehouseId);
        InventoryBalance destination = await db.InventoryBalances.SingleAsync(x => x.WarehouseId == p.DestinationWarehouseId);
        Assert.Equal((4m, 4m, 9m, 1m), (source.OnHandQuantity, source.ReservedQuantity, destination.OnHandQuantity, destination.ReservedQuantity));
        Assert.Equal(13m, source.OnHandQuantity + destination.OnHandQuantity);
        List<StockMovement> movements = await db.StockMovements.OrderBy(x => x.MovementType).ToListAsync();
        Assert.Equal(2, movements.Count);
        Assert.Contains(movements, x => x.MovementType == StockMovementType.TransferOut && x.ReferenceId == pending.Id.ToString("D"));
        Assert.Contains(movements, x => x.MovementType == StockMovementType.TransferIn && x.ReferenceId == pending.Id.ToString("D"));
        Assert.All(movements, x => Assert.Equal(completed.CompletedAtUtc, x.OccurredAtUtc));
    }

    [Fact]
    public async Task MultiItemCompletionFailure_RollsBackEverything()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var p = await CreatePosition(client, "atomic");
        ProductResponse second = await CreateProduct(client, "atomic-2");
        await Seed(factory, p.ProductId, p.SourceWarehouseId, p.SourceLocationId, 5m, 0m);
        await Seed(factory, second.Id, p.SourceWarehouseId, p.SourceLocationId, 5m, 0m);
        WarehouseTransferResponse pending = await Create(client, Request(p, [(p.ProductId, 5m), (second.Id, 5m)]));
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
            (await db.InventoryBalances.SingleAsync(x => x.ProductId == second.Id)).Issue(1m);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/warehouse-transfers/{pending.Id}/complete", null)).StatusCode);
        using IServiceScope verifyScope = factory.Services.CreateScope();
        InventoryWarehouseDbContext verify = verifyScope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        Assert.Equal(5m, (await verify.InventoryBalances.SingleAsync(x => x.ProductId == p.ProductId)).OnHandQuantity);
        Assert.False(await verify.StockMovements.AnyAsync());
        WarehouseTransfer transfer = await verify.WarehouseTransfers.Include(x => x.Items).SingleAsync();
        Assert.Equal(WarehouseTransferStatus.Pending, transfer.Status);
        Assert.All(transfer.Items, x => Assert.Null(x.TransferOutMovementId));
    }

    [Fact]
    public async Task PendingTransfer_ProtectsProductAndBothLocationsFromDeletion()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        var p = await CreatePosition(client, "delete");
        await Seed(factory, p.ProductId, p.SourceWarehouseId, p.SourceLocationId, 1m, 0m);
        await Create(client, Request(p, [(p.ProductId, 1m)]));
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
            db.InventoryBalances.RemoveRange(db.InventoryBalances);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/products/{p.ProductId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/warehouses/{p.SourceWarehouseId}/locations/{p.SourceLocationId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/warehouses/{p.DestinationWarehouseId}/locations/{p.DestinationLocationId}")).StatusCode);
    }

    private static CreateWarehouseTransferRequest Request(
        (Guid ProductId, Guid SourceWarehouseId, Guid SourceLocationId, Guid DestinationWarehouseId, Guid DestinationLocationId) p,
        (Guid ProductId, decimal Quantity)[] items) => new(p.SourceWarehouseId, p.SourceLocationId,
            p.DestinationWarehouseId, p.DestinationLocationId,
            items.Select(x => new CreateWarehouseTransferItemRequest(x.ProductId, x.Quantity)).ToList());

    private static async Task<WarehouseTransferResponse> Create(HttpClient client, CreateWarehouseTransferRequest request) =>
        (await (await client.PostAsJsonAsync("/api/warehouse-transfers", request)).Content.ReadFromJsonAsync<WarehouseTransferResponse>())!;

    private static async Task<(Guid ProductId, Guid SourceWarehouseId, Guid SourceLocationId, Guid DestinationWarehouseId, Guid DestinationLocationId)>
        CreatePosition(HttpClient client, string suffix)
    {
        ProductResponse product = await CreateProduct(client, suffix);
        WarehouseResponse source = await CreateWarehouse(client, "src-" + suffix);
        WarehouseResponse destination = await CreateWarehouse(client, "dst-" + suffix);
        WarehouseLocationResponse sourceLocation = await CreateLocation(client, source.Id, "S-" + suffix);
        WarehouseLocationResponse destinationLocation = await CreateLocation(client, destination.Id, "D-" + suffix);
        return (product.Id, source.Id, sourceLocation.Id, destination.Id, destinationLocation.Id);
    }

    private static async Task<ProductResponse> CreateProduct(HttpClient client, string suffix) =>
        (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest("sku-" + suffix, suffix, null))).Content.ReadFromJsonAsync<ProductResponse>())!;
    private static async Task<WarehouseResponse> CreateWarehouse(HttpClient client, string suffix) =>
        (await (await client.PostAsJsonAsync("/api/warehouses", new CreateWarehouseRequest("wh-" + suffix, suffix, null))).Content.ReadFromJsonAsync<WarehouseResponse>())!;
    private static async Task<WarehouseLocationResponse> CreateLocation(HttpClient client, Guid warehouseId, string suffix) =>
        (await (await client.PostAsJsonAsync($"/api/warehouses/{warehouseId}/locations", new CreateWarehouseLocationRequest(suffix, suffix, null))).Content.ReadFromJsonAsync<WarehouseLocationResponse>())!;
    private static async Task Seed(ApiFactory factory, Guid productId, Guid warehouseId, Guid locationId, decimal onHand, decimal reserved)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        db.InventoryBalances.Add(new(productId, warehouseId, locationId, onHand, reserved));
        await db.SaveChangesAsync();
    }
}
