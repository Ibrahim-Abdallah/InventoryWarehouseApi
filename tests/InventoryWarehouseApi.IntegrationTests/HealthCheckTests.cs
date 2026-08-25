using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class HealthCheckTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(ApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        using HttpResponseMessage response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenApiDocument_ReturnsOk()
    {
        using HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"Bearer\"", await response.Content.ReadAsStringAsync());
    }
}
