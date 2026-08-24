using FluentValidation;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.Warehouses;
using InventoryWarehouseApi.Application.WarehouseLocations;
using InventoryWarehouseApi.Application.Inventory;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IWarehouseLocationService, WarehouseLocationService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IStockMovementService, StockMovementService>();
        services.AddScoped<IInventoryAdjustmentService, InventoryAdjustmentService>();
        return services;
    }
}
