using FluentValidation;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.Warehouses;
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
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        return services;
    }
}
