using FluentValidation;
using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.Reporting;

internal sealed class InventoryReportingService(IInventoryReportingRepository repository,IValidator<InventorySummaryQuery> summaryValidator,IValidator<WarehouseInventoryQuery> warehouseValidator,IValidator<StockMovementReportQuery> movementValidator,IValidator<LowStockReportQuery> lowValidator,IValidator<ProductStockHistoryQuery> historyValidator):IInventoryReportingService
{
    public async Task<PagedResult<InventorySummaryItem>> ListInventorySummaryAsync(InventorySummaryQuery q,CancellationToken ct){await summaryValidator.ValidateAndThrowAsync(q,ct);return await repository.ListInventorySummaryAsync(q with { Search=Clean(q.Search) },ct);}
    public async Task<PagedResult<WarehouseInventoryItem>> ListWarehouseInventoryAsync(Guid id,WarehouseInventoryQuery q,CancellationToken ct){await warehouseValidator.ValidateAndThrowAsync(q,ct);if(!await repository.WarehouseExistsAsync(id,ct))throw new NotFoundException("Warehouse was not found.");return await repository.ListWarehouseInventoryAsync(id,q with { Search=Clean(q.Search) },ct);}
    public async Task<PagedResult<StockMovementReportItem>> ListStockMovementsAsync(StockMovementReportQuery q,CancellationToken ct){await movementValidator.ValidateAndThrowAsync(q,ct);return await repository.ListStockMovementsAsync(Normalize(q),ct);}
    public async Task<PagedResult<LowStockReportItem>> ListLowStockAsync(LowStockReportQuery q,CancellationToken ct){await lowValidator.ValidateAndThrowAsync(q,ct);return await repository.ListLowStockAsync(q with { Search=Clean(q.Search) },ct);}
    public async Task<PagedResult<ProductStockHistoryItem>> ListProductStockHistoryAsync(Guid id,ProductStockHistoryQuery q,CancellationToken ct){await historyValidator.ValidateAndThrowAsync(q,ct);if(!await repository.ProductExistsAsync(id,ct))throw new NotFoundException("Product was not found.");return await repository.ListProductStockHistoryAsync(id,q with{FromUtc=q.FromUtc?.ToUniversalTime(),ToUtc=q.ToUtc?.ToUniversalTime()},ct);}
    private static StockMovementReportQuery Normalize(StockMovementReportQuery q)=>q with{ReferenceType=Clean(q.ReferenceType),ReferenceId=Clean(q.ReferenceId),FromUtc=q.FromUtc?.ToUniversalTime(),ToUtc=q.ToUtc?.ToUniversalTime()};
    private static string? Clean(string? s)=>string.IsNullOrWhiteSpace(s)?null:s.Trim();
}
