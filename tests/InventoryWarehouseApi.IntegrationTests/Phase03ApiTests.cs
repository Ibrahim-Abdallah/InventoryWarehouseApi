using System.Net;
using System.Net.Http.Json;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.WarehouseLocations;
using InventoryWarehouseApi.Application.Warehouses;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase03ApiTests
{
    [Fact]
    public async Task Locations_Crud_Normalization_Conflict_AndSafeWarehouseDelete()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        WarehouseResponse warehouse = await CreateWarehouse(client, "wh-03");
        HttpResponseMessage created = await client.PostAsJsonAsync($"/api/warehouses/{warehouse.Id}/locations", new CreateWarehouseLocationRequest(" a-01 ", " Shelf A ", " "));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        WarehouseLocationResponse location = (await created.Content.ReadFromJsonAsync<WarehouseLocationResponse>())!;
        Assert.Equal("A-01", location.Code);
        Assert.Null(location.Description);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/warehouses/{warehouse.Id}/locations", new CreateWarehouseLocationRequest("a-01", "Duplicate", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/warehouses/{warehouse.Id}")).StatusCode);
        WarehouseLocationResponse updated = (await (await client.PutAsJsonAsync($"/api/warehouses/{warehouse.Id}/locations/{location.Id}", new UpdateWarehouseLocationRequest(" a-01 ", "Updated", null, false))).Content.ReadFromJsonAsync<WarehouseLocationResponse>())!;
        Assert.False(updated.IsActive);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/warehouses/{warehouse.Id}/locations/{location.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/warehouses/{warehouse.Id}/locations/{location.Id}")).StatusCode);
    }

    [Fact]
    public async Task Inventory_ReadsAggregateLeftJoinAndProtectsDependencies()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        ProductResponse product = await CreateProduct(client, "sku-03");
        WarehouseResponse warehouse = await CreateWarehouse(client, "wh-inv");
        WarehouseLocationResponse a = await CreateLocation(client, warehouse.Id, "A");
        WarehouseLocationResponse b = await CreateLocation(client, warehouse.Id, "B");
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
            db.InventoryBalances.AddRange(new InventoryBalance(product.Id, warehouse.Id, a.Id, 10m, 3m), new InventoryBalance(product.Id, warehouse.Id, b.Id, 5m, 1m));
            await db.SaveChangesAsync();
        }
        WarehouseInventoryResponse summary = (await client.GetFromJsonAsync<WarehouseInventoryResponse>($"/api/inventory/products/{product.Id}/warehouses/{warehouse.Id}"))!;
        Assert.Equal((15m, 4m, 11m), (summary.OnHandQuantity, summary.ReservedQuantity, summary.AvailableQuantity));
        PagedResult<LocationInventoryResponse> list = (await client.GetFromJsonAsync<PagedResult<LocationInventoryResponse>>($"/api/inventory/products/{product.Id}/warehouses/{warehouse.Id}/locations?pageSize=1"))!;
        Assert.Equal(2, list.TotalCount);
        Assert.Single(list.Items);
        LocationInventoryResponse detail = (await client.GetFromJsonAsync<LocationInventoryResponse>($"/api/inventory/products/{product.Id}/warehouses/{warehouse.Id}/locations/{a.Id}"))!;
        Assert.Equal(7m, detail.AvailableQuantity);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/products/{product.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/warehouses/{warehouse.Id}/locations/{a.Id}")).StatusCode);
    }

    private static async Task<ProductResponse> CreateProduct(HttpClient client, string sku) =>
        (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest(sku, sku, null))).Content.ReadFromJsonAsync<ProductResponse>())!;
    private static async Task<WarehouseResponse> CreateWarehouse(HttpClient client, string code) =>
        (await (await client.PostAsJsonAsync("/api/warehouses", new CreateWarehouseRequest(code, code, null))).Content.ReadFromJsonAsync<WarehouseResponse>())!;
    private static async Task<WarehouseLocationResponse> CreateLocation(HttpClient client, Guid warehouseId, string code) =>
        (await (await client.PostAsJsonAsync($"/api/warehouses/{warehouseId}/locations", new CreateWarehouseLocationRequest(code, code, null))).Content.ReadFromJsonAsync<WarehouseLocationResponse>())!;
}
