using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Products;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase11ErrorHandlingTests
{
    [Fact]
    public async Task ExpectedFailures_UseProblemDetailsWithValidationErrors()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage validation = await client.PostAsJsonAsync("/api/products", new CreateProductRequest("", "", null));
        HttpResponseMessage missing = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.BadRequest, validation.StatusCode);
        JsonElement validationBody = await validation.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Validation failed", validationBody.GetProperty("title").GetString());
        Assert.True(validationBody.GetProperty("errors").EnumerateObject().Any());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("Resource not found", (await missing.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
    }

    [Fact]
    public async Task UnexpectedException_ReturnsSanitizedProblemDetails()
    {
        using ApiFactory factory = new(false, services =>
        {
            services.RemoveAll<IProductService>();
            services.AddScoped<IProductService, ThrowingProductService>();
        });
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/products");
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        JsonElement problem = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("An unexpected error occurred", problem.GetProperty("title").GetString());
        Assert.Equal("An unexpected error occurred while processing the request.", problem.GetProperty("detail").GetString());
        Assert.DoesNotContain("SENSITIVE-INTERNAL-TEST-MESSAGE", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MalformedReportSortDirection_ReturnsValidationProblemRatherThan500()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/reports/inventory-summary?sortDirection=sideways");
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Validation failed", problem.GetProperty("title").GetString());
    }

    private sealed class ThrowingProductService : IProductService
    {
        private static InvalidOperationException Failure() => new("SENSITIVE-INTERNAL-TEST-MESSAGE");
        public Task<PagedResult<ProductResponse>> ListAsync(ProductQuery query, CancellationToken cancellationToken) => throw Failure();
        public Task<ProductResponse> GetAsync(Guid id, CancellationToken cancellationToken) => throw Failure();
        public Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken) => throw Failure();
        public Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken) => throw Failure();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw Failure();
    }
}
