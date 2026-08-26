using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase12OpenApiTests
{
    private const string Password = "StrongPassword1!";

    [Fact]
    public async Task OpenApi_RepresentativeOperationsExposeProfessionalMetadata()
    {
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        using JsonDocument document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        JsonElement root = document.RootElement;

        Assert.Equal("Inventory Warehouse API", root.GetProperty("info").GetProperty("title").GetString());
        List<(string Path, string Method, JsonElement Operation)> controllerOperations = [];
        foreach (JsonProperty path in root.GetProperty("paths").EnumerateObject())
        {
            if (!path.Name.StartsWith("/api/", StringComparison.Ordinal)) continue;
            foreach (JsonProperty method in path.Value.EnumerateObject())
                if (method.Name is "get" or "post" or "put" or "delete" or "patch")
                    controllerOperations.Add((path.Name, method.Name, method.Value));
        }
        Assert.Equal(52, controllerOperations.Count);
        Assert.All(controllerOperations, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Operation.GetProperty("summary").GetString()), $"Missing summary for {item.Method} {item.Path}.");
            Assert.False(string.IsNullOrWhiteSpace(item.Operation.GetProperty("description").GetString()), $"Missing description for {item.Method} {item.Path}.");
            Assert.Contains(item.Operation.GetProperty("responses").EnumerateObject(), response => response.Name.StartsWith('2'));
        });
        AssertOperation(root, "/api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}/adjustments/increase", "post", "Increase inventory adjustment", "200", "400", "401", "403", "404", "409", "500");
        AssertOperation(root, "/api/auth/login", "post", "Log in", "200", "400", "401");
        AssertOperation(root, "/api/warehouse-transfers/{id}/complete", "post", "Complete warehouse transfer", "200", "400", "401", "403", "404", "409", "500");
        AssertOperation(root, "/api/reports/inventory-summary", "get", "Get inventory summary report", "200", "400", "401", "403", "500");

        JsonElement adjustment = Operation(root, "/api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}/adjustments/increase", "post");
        Assert.Contains("audited inventory increase", adjustment.GetProperty("description").GetString());
        Assert.Contains("InventoryAdjustmentOperationResponse", SchemaReference(adjustment.GetProperty("responses").GetProperty("200")));
        Assert.Contains("ProblemDetails", SchemaReference(adjustment.GetProperty("responses").GetProperty("409")));
    }

    [Fact]
    public async Task JwtChallenge_ReturnsUnauthorizedProblemDetails()
    {
        using ApiFactory factory = new(true);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Headers.WwwAuthenticate.SingleOrDefault(x => x.Scheme == "Bearer"));
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Unauthorized", problem?.Title);
        Assert.Equal(401, problem?.Status);
    }

    [Fact]
    public async Task JwtForbid_ReturnsForbiddenProblemDetails()
    {
        using ApiFactory factory = new(true);
        User viewer = await Seed(factory, "phase12-viewer@tests.local", UserRole.Viewer);
        using HttpClient anonymous = factory.CreateClient();
        AuthTokenResponse auth = await Login(anonymous, viewer.Email);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        using HttpResponseMessage response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Forbidden", problem?.Title);
        Assert.Equal(403, problem?.Status);
    }

    private static void AssertOperation(JsonElement root, string path, string method, string summary, params string[] statuses)
    {
        JsonElement operation = Operation(root, path, method);
        Assert.Equal(summary, operation.GetProperty("summary").GetString());
        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
        JsonElement responses = operation.GetProperty("responses");
        foreach (string status in statuses) Assert.True(responses.TryGetProperty(status, out _), $"Missing {status} response for {method.ToUpperInvariant()} {path}.");
        Assert.False(string.IsNullOrWhiteSpace(SchemaReference(responses.GetProperty("200"))));
    }

    private static JsonElement Operation(JsonElement root, string path, string method) =>
        root.GetProperty("paths").GetProperty(path).GetProperty(method);

    private static string SchemaReference(JsonElement response)
    {
        JsonElement content = response.GetProperty("content");
        JsonElement mediaType = content.TryGetProperty("application/problem+json", out JsonElement problem)
            ? problem
            : content.GetProperty("application/json");
        JsonElement schema = mediaType.GetProperty("schema");
        return schema.TryGetProperty("$ref", out JsonElement reference) ? reference.GetString() ?? "" : schema.ToString();
    }

    private static async Task<AuthTokenResponse> Login(HttpClient client, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenResponse>())!;
    }

    private static async Task<User> Seed(ApiFactory factory, string email, UserRole role)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        IPasswordHashService passwords = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        User draft = new(Guid.NewGuid(), email, "Phase 12 User", "pending", role, now);
        User user = new(draft.Id, email, draft.DisplayName, passwords.HashPassword(draft, Password), role, now);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
