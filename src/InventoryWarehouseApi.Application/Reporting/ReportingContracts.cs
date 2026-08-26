using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.Reporting;

public interface IInventoryReportingRepository
{
    Task<bool> ProductExistsAsync(Guid id,CancellationToken ct);
    Task<bool> WarehouseExistsAsync(Guid id,CancellationToken ct);
    Task<PagedResult<InventorySummaryItem>> ListInventorySummaryAsync(InventorySummaryQuery query,CancellationToken ct);
    Task<PagedResult<WarehouseInventoryItem>> ListWarehouseInventoryAsync(Guid warehouseId,WarehouseInventoryQuery query,CancellationToken ct);
    Task<PagedResult<StockMovementReportItem>> ListStockMovementsAsync(StockMovementReportQuery query,CancellationToken ct);
    Task<PagedResult<LowStockReportItem>> ListLowStockAsync(LowStockReportQuery query,CancellationToken ct);
    Task<PagedResult<ProductStockHistoryItem>> ListProductStockHistoryAsync(Guid productId,ProductStockHistoryQuery query,CancellationToken ct);
}

public interface IInventoryReportingService
{
    Task<PagedResult<InventorySummaryItem>> ListInventorySummaryAsync(InventorySummaryQuery query,CancellationToken ct);
    Task<PagedResult<WarehouseInventoryItem>> ListWarehouseInventoryAsync(Guid warehouseId,WarehouseInventoryQuery query,CancellationToken ct);
    Task<PagedResult<StockMovementReportItem>> ListStockMovementsAsync(StockMovementReportQuery query,CancellationToken ct);
    Task<PagedResult<LowStockReportItem>> ListLowStockAsync(LowStockReportQuery query,CancellationToken ct);
    Task<PagedResult<ProductStockHistoryItem>> ListProductStockHistoryAsync(Guid productId,ProductStockHistoryQuery query,CancellationToken ct);
}
