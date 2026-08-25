namespace InventoryWarehouseApi.Api;

using InventoryWarehouseApi.Api.BackgroundJobs;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddHealthChecks();
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddOptions<LowStockMonitoringOptions>().BindConfiguration(LowStockMonitoringOptions.SectionName)
            .Validate(x => !x.Enabled || x.IntervalSeconds is >= 5 and <= 86400, "IntervalSeconds must be between 5 and 86400 when enabled.").ValidateOnStart();
        services.AddHostedService<LowStockMonitoringWorker>();

        return services;
    }
}
