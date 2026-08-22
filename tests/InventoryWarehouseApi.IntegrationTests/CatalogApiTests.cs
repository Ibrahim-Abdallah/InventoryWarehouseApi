using System.Net;
using System.Net.Http.Json;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.Warehouses;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class CatalogApiTests
{
    [Fact]
    public async Task Products_SupportCrudAndCreateConflicts()
    {
        await using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        ProductResponse product = await CreateProduct(client, " sku-002 ", "Beta");

        Assert.Equal("SKU-002", product.Sku);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/products/{product.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/products", new CreateProductRequest("SKU-002", "Duplicate", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/products", new CreateProductRequest("sku-002", "Duplicate", null))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/products", new CreateProductRequest("", "", null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/products/{Guid.NewGuid()}")).StatusCode);

        HttpResponseMessage updated = await client.PutAsJsonAsync($"/api/products/{product.Id}", new UpdateProductRequest("SKU-002", "Updated", null, false));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.False((await updated.Content.ReadFromJsonAsync<ProductResponse>())!.IsActive);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/products/{product.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/products/{product.Id}")).StatusCode);
    }

    [Fact]
    public async Task Products_QueryControlsFilterPageAndSortInBothDirections()
    {
        await using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await CreateProduct(client, "SKU-002", "Alpha Match");
        await CreateProduct(client, "SKU-001", "Bravo Match");
        ProductResponse inactive = await CreateProduct(client, "SKU-003", "Inactive Match");
        await client.PutAsJsonAsync($"/api/products/{inactive.Id}", new UpdateProductRequest(inactive.Sku, inactive.Name, null, false));
        await CreateProduct(client, "X-999", "Nonmatching Zulu");

        PagedResult<ProductResponse> search = await GetProducts(client, "search=Alpha&sortBy=sku");
        Assert.Single(search.Items);
        Assert.Equal("SKU-002", search.Items[0].Sku);
        Assert.Equal(1, search.TotalCount);

        PagedResult<ProductResponse> active = await GetProducts(client, "isActive=true&pageSize=2&sortBy=sku&sortDirection=asc");
        Assert.Equal(2, active.Items.Count);
        Assert.Equal(3, active.TotalCount);
        Assert.Equal(["SKU-001", "SKU-002"], active.Items.Select(x => x.Sku));

        PagedResult<ProductResponse> inactivePage = await GetProducts(client, "isActive=false&sortBy=sku");
        Assert.Single(inactivePage.Items);
        Assert.Equal("SKU-003", inactivePage.Items[0].Sku);
        Assert.Equal(1, inactivePage.TotalCount);

        PagedResult<ProductResponse> descending = await GetProducts(client, "isActive=true&sortBy=sku&sortDirection=desc");
        Assert.Equal(["X-999", "SKU-002", "SKU-001"], descending.Items.Select(x => x.Sku));
    }

    [Fact]
    public async Task ProductUpdate_AllowsOwnSkuButRejectsAnotherSkuRegardlessOfCase()
    {
        await using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        ProductResponse first = await CreateProduct(client, "SKU-A", "First");
        await CreateProduct(client, "SKU-B", "Second");

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/products/{first.Id}", new UpdateProductRequest("sku-a", "First", null, true))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/products/{first.Id}", new UpdateProductRequest("sku-b", "First", null, true))).StatusCode);
    }

    [Fact]
    public async Task Warehouses_SupportCrudAndCreateConflicts()
    {
        await using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        WarehouseResponse warehouse = await CreateWarehouse(client, " wh-02 ", "Delta");

        Assert.Equal("WH-02", warehouse.Code);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/warehouses/{warehouse.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/warehouses", new CreateWarehouseRequest("WH-02", "Duplicate", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/warehouses", new CreateWarehouseRequest("wh-02", "Duplicate", null))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/warehouses", new CreateWarehouseRequest("", "", null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/warehouses/{Guid.NewGuid()}")).StatusCode);
        HttpResponseMessage updated = await client.PutAsJsonAsync($"/api/warehouses/{warehouse.Id}", new UpdateWarehouseRequest("WH-02", "Updated", null, false));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.False((await updated.Content.ReadFromJsonAsync<WarehouseResponse>())!.IsActive);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/warehouses/{warehouse.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/warehouses/{warehouse.Id}")).StatusCode);
    }

    [Fact]
    public async Task Warehouses_QueryControlsFilterPageAndSortInBothDirections()
    {
        await using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await CreateWarehouse(client, "WH-02", "Alpha Match");
        await CreateWarehouse(client, "WH-01", "Bravo Match");
        WarehouseResponse inactive = await CreateWarehouse(client, "WH-03", "Inactive Match");
        await client.PutAsJsonAsync($"/api/warehouses/{inactive.Id}", new UpdateWarehouseRequest(inactive.Code, inactive.Name, null, false));
        await CreateWarehouse(client, "ZZ-99", "Nonmatching Zulu");

        PagedResult<WarehouseResponse> search = await GetWarehouses(client, "search=Alpha&sortBy=code");
        Assert.Single(search.Items);
        Assert.Equal("WH-02", search.Items[0].Code);
        Assert.Equal(1, search.TotalCount);
        PagedResult<WarehouseResponse> active = await GetWarehouses(client, "isActive=true&pageSize=2&sortBy=code&sortDirection=asc");
        Assert.Equal(2, active.Items.Count);
        Assert.Equal(3, active.TotalCount);
        Assert.Equal(["WH-01", "WH-02"], active.Items.Select(x => x.Code));
        PagedResult<WarehouseResponse> inactivePage = await GetWarehouses(client, "isActive=false&sortBy=code");
        Assert.Single(inactivePage.Items);
        Assert.Equal("WH-03", inactivePage.Items[0].Code);
        Assert.Equal(1, inactivePage.TotalCount);
        PagedResult<WarehouseResponse> descending = await GetWarehouses(client, "isActive=true&sortBy=code&sortDirection=desc");
        Assert.Equal(["ZZ-99", "WH-02", "WH-01"], descending.Items.Select(x => x.Code));
    }

    [Fact]
    public async Task WarehouseUpdate_AllowsOwnCodeButRejectsAnotherCodeRegardlessOfCase()
    {
        await using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        WarehouseResponse first = await CreateWarehouse(client, "WH-A", "First");
        await CreateWarehouse(client, "WH-B", "Second");
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/warehouses/{first.Id}", new UpdateWarehouseRequest("wh-a", "First", null, true))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/warehouses/{first.Id}", new UpdateWarehouseRequest("wh-b", "First", null, true))).StatusCode);
    }

    private static async Task<ProductResponse> CreateProduct(HttpClient client, string sku, string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(sku, name, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }
    private static async Task<WarehouseResponse> CreateWarehouse(HttpClient client, string code, string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/warehouses", new CreateWarehouseRequest(code, name, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<WarehouseResponse>())!;
    }
    private static Task<PagedResult<ProductResponse>> GetProducts(HttpClient client, string query) =>
        client.GetFromJsonAsync<PagedResult<ProductResponse>>($"/api/products?{query}")!;
    private static Task<PagedResult<WarehouseResponse>> GetWarehouses(HttpClient client, string query) =>
        client.GetFromJsonAsync<PagedResult<WarehouseResponse>>($"/api/warehouses?{query}")!;
}
