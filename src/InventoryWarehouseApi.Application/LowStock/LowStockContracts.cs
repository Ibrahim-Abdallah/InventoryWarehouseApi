using InventoryWarehouseApi.Application.Common;
namespace InventoryWarehouseApi.Application.LowStock;
public interface ILowStockRepository
{
 Task<LowStockThresholdResponse> UpsertThresholdAsync(Guid productId,Guid warehouseId,Guid locationId,UpsertLowStockThresholdRequest request,DateTimeOffset now,CancellationToken ct);
 Task<LowStockThresholdResponse?> GetThresholdAsync(Guid productId,Guid warehouseId,Guid locationId,CancellationToken ct);
 Task<PagedResult<LowStockThresholdResponse>> ListThresholdsAsync(LowStockThresholdQuery query,CancellationToken ct);
 Task<PagedResult<LowStockResponse>> ListLowStockAsync(LowStockQuery query,CancellationToken ct);
 Task<PagedResult<LowStockAlertResponse>> ListAlertsAsync(LowStockAlertQuery query,CancellationToken ct);
 Task<LowStockMonitoringRunResult> ReconcileAsync(DateTimeOffset observedAtUtc,CancellationToken ct);
}
public interface ILowStockService
{
 Task<LowStockThresholdResponse> UpsertThresholdAsync(Guid productId,Guid warehouseId,Guid locationId,UpsertLowStockThresholdRequest request,CancellationToken ct);
 Task<LowStockThresholdResponse> GetThresholdAsync(Guid productId,Guid warehouseId,Guid locationId,CancellationToken ct);
 Task<PagedResult<LowStockThresholdResponse>> ListThresholdsAsync(LowStockThresholdQuery query,CancellationToken ct);
 Task<PagedResult<LowStockResponse>> ListLowStockAsync(LowStockQuery query,CancellationToken ct);
 Task<PagedResult<LowStockAlertResponse>> ListAlertsAsync(LowStockAlertQuery query,CancellationToken ct);
}
public interface ILowStockMonitoringService { Task<LowStockMonitoringRunResult> RunAsync(DateTimeOffset observedAtUtc,CancellationToken ct); }
