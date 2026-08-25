using FluentValidation;
using InventoryWarehouseApi.Application.Common;
namespace InventoryWarehouseApi.Application.LowStock;
internal sealed class LowStockService(ILowStockRepository repository,IValidator<UpsertLowStockThresholdRequest> upsert,IValidator<LowStockThresholdQuery> tq,IValidator<LowStockQuery> lq,IValidator<LowStockAlertQuery> aq):ILowStockService
{
 public async Task<LowStockThresholdResponse> UpsertThresholdAsync(Guid p,Guid w,Guid l,UpsertLowStockThresholdRequest r,CancellationToken ct){ValidateIds(p,w,l);await upsert.ValidateAndThrowAsync(r,ct);return await repository.UpsertThresholdAsync(p,w,l,r,DateTimeOffset.UtcNow,ct);}
 public async Task<LowStockThresholdResponse> GetThresholdAsync(Guid p,Guid w,Guid l,CancellationToken ct){ValidateIds(p,w,l);return await repository.GetThresholdAsync(p,w,l,ct)??throw new NotFoundException("Low-stock threshold was not found.");}
 public async Task<PagedResult<LowStockThresholdResponse>> ListThresholdsAsync(LowStockThresholdQuery q,CancellationToken ct){await tq.ValidateAndThrowAsync(q,ct);return await repository.ListThresholdsAsync(q,ct);}
 public async Task<PagedResult<LowStockResponse>> ListLowStockAsync(LowStockQuery q,CancellationToken ct){await lq.ValidateAndThrowAsync(q,ct);return await repository.ListLowStockAsync(q,ct);}
 public async Task<PagedResult<LowStockAlertResponse>> ListAlertsAsync(LowStockAlertQuery q,CancellationToken ct){await aq.ValidateAndThrowAsync(q,ct);return await repository.ListAlertsAsync(q,ct);}
 private static void ValidateIds(params Guid[] ids){if(ids.Any(x=>x==Guid.Empty))throw new ValidationException("Route IDs cannot be empty.");}
}
internal sealed class LowStockMonitoringService(ILowStockRepository repository):ILowStockMonitoringService { public Task<LowStockMonitoringRunResult> RunAsync(DateTimeOffset t,CancellationToken ct)=>repository.ReconcileAsync(t.ToUniversalTime(),ct); }
