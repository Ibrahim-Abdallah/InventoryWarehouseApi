using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.Warehouses;
using InventoryWarehouseApi.Application.WarehouseLocations;
using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Infrastructure.Persistence;
using InventoryWarehouseApi.Application.WarehouseTransfers;
using InventoryWarehouseApi.Application.InventoryReservations;
using InventoryWarehouseApi.Application.LowStock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Infrastructure.Security;

namespace InventoryWarehouseApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        services.AddDbContext<InventoryWarehouseDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IWarehouseLocationRepository, WarehouseLocationRepository>();
        services.AddScoped<IInventoryQueryRepository, InventoryQueryRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IInventoryAdjustmentRepository, InventoryAdjustmentRepository>();
        services.AddScoped<IWarehouseTransferRepository, WarehouseTransferRepository>();
        services.AddScoped<IInventoryReservationRepository, InventoryReservationRepository>();
        services.AddScoped<ILowStockRepository, LowStockRepository>();
        services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        return services;
    }
}
