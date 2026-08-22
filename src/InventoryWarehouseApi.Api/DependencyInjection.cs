namespace InventoryWarehouseApi.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddHealthChecks();
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();

        return services;
    }
}
