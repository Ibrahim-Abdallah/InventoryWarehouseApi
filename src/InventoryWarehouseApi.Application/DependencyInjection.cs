using FluentValidation;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.Warehouses;
using InventoryWarehouseApi.Application.WarehouseLocations;
using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Application.WarehouseTransfers;
using InventoryWarehouseApi.Application.InventoryReservations;
using InventoryWarehouseApi.Application.LowStock;
using Microsoft.Extensions.DependencyInjection;
using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Application.Reporting;

namespace InventoryWarehouseApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateProductRequest>, CreateProductValidator>();
        services.AddScoped<IValidator<UpdateProductRequest>, UpdateProductValidator>();
        services.AddScoped<IValidator<ProductQuery>, ProductQueryValidator>();
        services.AddScoped<IValidator<CreateWarehouseRequest>, CreateWarehouseValidator>();
        services.AddScoped<IValidator<UpdateWarehouseRequest>, UpdateWarehouseValidator>();
        services.AddScoped<IValidator<WarehouseQuery>, WarehouseQueryValidator>();
        services.AddScoped<IValidator<CreateWarehouseLocationRequest>, CreateWarehouseLocationValidator>();
        services.AddScoped<IValidator<UpdateWarehouseLocationRequest>, UpdateWarehouseLocationValidator>();
        services.AddScoped<IValidator<WarehouseLocationQuery>, WarehouseLocationQueryValidator>();
        services.AddScoped<IValidator<LocationInventoryQuery>, LocationInventoryQueryValidator>();
        services.AddScoped<IValidator<StockMovementRequest>, StockMovementRequestValidator>();
        services.AddScoped<IValidator<StockMovementHistoryQuery>, StockMovementHistoryQueryValidator>();
        services.AddScoped<IValidator<InventoryAdjustmentRequest>, InventoryAdjustmentRequestValidator>();
        services.AddScoped<IValidator<InventoryAdjustmentHistoryQuery>, InventoryAdjustmentHistoryQueryValidator>();
        services.AddScoped<IValidator<CreateWarehouseTransferRequest>, CreateWarehouseTransferValidator>();
        services.AddScoped<IValidator<WarehouseTransferHistoryQuery>, WarehouseTransferHistoryQueryValidator>();
        services.AddScoped<IValidator<CreateInventoryReservationRequest>, CreateInventoryReservationValidator>();
        services.AddScoped<IValidator<InventoryReservationHistoryQuery>, InventoryReservationHistoryQueryValidator>();
        services.AddScoped<IValidator<UpsertLowStockThresholdRequest>, UpsertLowStockThresholdValidator>();
        services.AddScoped<IValidator<LowStockThresholdQuery>, LowStockThresholdQueryValidator>();
        services.AddScoped<IValidator<LowStockQuery>, LowStockQueryValidator>();
        services.AddScoped<IValidator<LowStockAlertQuery>, LowStockAlertQueryValidator>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IWarehouseLocationService, WarehouseLocationService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IStockMovementService, StockMovementService>();
        services.AddScoped<IInventoryAdjustmentService, InventoryAdjustmentService>();
        services.AddScoped<IWarehouseTransferService, WarehouseTransferService>();
        services.AddScoped<IInventoryReservationService, InventoryReservationService>();
        services.AddScoped<ILowStockService, LowStockService>();
        services.AddScoped<ILowStockMonitoringService, LowStockMonitoringService>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<RefreshTokenRequest>, RefreshTokenRequestValidator>();
        services.AddScoped<IValidator<LogoutRequest>, LogoutRequestValidator>();
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<UpdateUserRoleRequest>, UpdateUserRoleRequestValidator>();
        services.AddScoped<IValidator<UserQuery>, UserQueryValidator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IValidator<InventorySummaryQuery>, InventorySummaryQueryValidator>();
        services.AddScoped<IValidator<WarehouseInventoryQuery>, WarehouseInventoryQueryValidator>();
        services.AddScoped<IValidator<StockMovementReportQuery>, StockMovementReportQueryValidator>();
        services.AddScoped<IValidator<LowStockReportQuery>, LowStockReportQueryValidator>();
        services.AddScoped<IValidator<ProductStockHistoryQuery>, ProductStockHistoryQueryValidator>();
        services.AddScoped<IInventoryReportingService, InventoryReportingService>();
        return services;
    }
}
