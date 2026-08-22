using System.Net;
using System.Net.Http.Json;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.Warehouses;
using InventoryWarehouseApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class ValidationAndPersistenceTests
{
    [Theory]
    [InlineData("/api/products?pageNumber=0")]
    [InlineData("/api/products?pageSize=101")]
    [InlineData("/api/products?sortBy=unsupported")]
    [InlineData("/api/warehouses?sortDirection=sideways")]
    public async Task InvalidCollectionQuery_ReturnsBadRequestProblemDetails(string path)
    {
        await using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        ProblemDetails problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal(400, problem.Status);
        Assert.Equal("Validation failed", problem.Title);
    }

    [Fact]
    public async Task ProductRepository_TranslatesDatabaseUniqueViolationToConflict()
    {
        await using ApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IProductRepository repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await repository.AddAsync(new Product("db-sku", "First", null, now), CancellationToken.None);
        await repository.AddAsync(new Product("DB-SKU", "Second", null, now), CancellationToken.None);

        ConflictException exception = await Assert.ThrowsAsync<ConflictException>(() =>
            repository.SaveChangesAsync(CancellationToken.None));

        Assert.Contains("SKU", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WarehouseRepository_TranslatesDatabaseUniqueViolationToConflict()
    {
        await using ApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IWarehouseRepository repository = scope.ServiceProvider.GetRequiredService<IWarehouseRepository>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await repository.AddAsync(new Warehouse("db-code", "First", null, now), CancellationToken.None);
        await repository.AddAsync(new Warehouse("DB-CODE", "Second", null, now), CancellationToken.None);

        ConflictException exception = await Assert.ThrowsAsync<ConflictException>(() =>
            repository.SaveChangesAsync(CancellationToken.None));

        Assert.Contains("code", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
