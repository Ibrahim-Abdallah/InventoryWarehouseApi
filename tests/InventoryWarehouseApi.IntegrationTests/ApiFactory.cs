using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication;

namespace InventoryWarehouseApi.IntegrationTests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool useRealAuthentication;
    public ApiFactory() : this(false) { }
    internal ApiFactory(bool useRealAuthentication) => this.useRealAuthentication = useRealAuthentication;
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LowStockMonitoring:Enabled", "false");
        builder.UseSetting("Authentication:Jwt:Issuer", "InventoryWarehouseApi.Tests");
        builder.UseSetting("Authentication:Jwt:Audience", "InventoryWarehouseApi.Tests.Client");
        builder.UseSetting("Authentication:Jwt:SigningKey", "TEST-ONLY-SIGNING-KEY-IS-AT-LEAST-THIRTY-TWO-BYTES-LONG");
        builder.UseSetting("Authentication:DevelopmentAdmin:Enabled", "false");
        _connection.Open();
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<InventoryWarehouseDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<InventoryWarehouseDbContext>>();
            services.AddDbContext<InventoryWarehouseDbContext>(options => options.UseSqlite(_connection));
            if (!useRealAuthentication)
            {
                services.AddAuthentication(options => { options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName; options.DefaultChallengeScheme = TestAuthHandler.SchemeName; })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            }
            using IServiceScope scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
