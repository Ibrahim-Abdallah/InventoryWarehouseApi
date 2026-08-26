using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase11AuthorizationTests
{
    private const string Password = "StrongPassword1!";

    [Fact]
    public async Task RoleChangeAndDeactivation_RevokeRefreshTokens_AndReactivationRestoresLogin()
    {
        using ApiFactory factory = new(true);
        User admin = await Seed(factory, "admin-revocation@tests.local", UserRole.Admin);
        User user = await Seed(factory, "user-revocation@tests.local", UserRole.Viewer);
        using HttpClient anonymous = factory.CreateClient();
        AuthTokenResponse adminAuth = await Login(anonymous, admin.Email);
        AuthTokenResponse firstUserAuth = await Login(anonymous, user.Email);
        using HttpClient adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        Assert.Equal(HttpStatusCode.OK, (await adminClient.PutAsJsonAsync($"/api/users/{user.Id}/role", new UpdateUserRoleRequest(UserRole.WarehouseOperator))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(firstUserAuth.RefreshToken))).StatusCode);

        AuthTokenResponse secondUserAuth = await Login(anonymous, user.Email);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PutAsJsonAsync($"/api/users/{user.Id}/status", new UpdateUserStatusRequest(false))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(secondUserAuth.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, Password))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await adminClient.PutAsJsonAsync($"/api/users/{user.Id}/status", new UpdateUserStatusRequest(true))).StatusCode);
        Assert.Equal(user.Id, (await Login(anonymous, user.Email)).User.Id);
    }

    [Fact]
    public async Task LastActiveAdminSafeguard_BlocksLastButAllowsMutationWhenAnotherAdminRemains()
    {
        using ApiFactory factory = new(true);
        User first = await Seed(factory, "first-admin@tests.local", UserRole.Admin);
        using HttpClient anonymous = factory.CreateClient();
        AuthTokenResponse auth = await Login(anonymous, first.Email);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/users/{first.Id}/status", new UpdateUserStatusRequest(false))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/users/{first.Id}/role", new UpdateUserRoleRequest(UserRole.Viewer))).StatusCode);

        await Seed(factory, "second-admin@tests.local", UserRole.Admin);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/users/{first.Id}/status", new UpdateUserStatusRequest(false))).StatusCode);
        using IServiceScope scope = factory.Services.CreateScope();
        InventoryWarehouseDbContext db = scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();
        Assert.Equal(1, await db.Users.CountAsync(x => x.IsActive && x.Role == UserRole.Admin));
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
        User draft = new(Guid.NewGuid(), email, "Phase 11 User", "pending", role, now);
        User user = new(draft.Id, email, draft.DisplayName, passwords.HashPassword(draft, Password), role, now);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
