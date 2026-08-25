namespace InventoryWarehouseApi.Application.LowStock;
public sealed record UpsertLowStockThresholdRequest(decimal ThresholdQuantity, bool IsEnabled);
public sealed record LowStockThresholdResponse(Guid Id, Guid ProductId, Guid WarehouseId, Guid WarehouseLocationId, decimal ThresholdQuantity, bool IsEnabled, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record LowStockThresholdQuery(int PageNumber=1,int PageSize=20,Guid? ProductId=null,Guid? WarehouseId=null,bool? IsEnabled=null);
public sealed record LowStockQuery(int PageNumber=1,int PageSize=20,Guid? ProductId=null,Guid? WarehouseId=null);
public sealed record LowStockResponse(Guid ThresholdId,Guid ProductId,string ProductSku,string ProductName,Guid WarehouseId,string WarehouseCode,string WarehouseName,Guid WarehouseLocationId,string WarehouseLocationCode,string WarehouseLocationName,decimal ThresholdQuantity,decimal OnHandQuantity,decimal ReservedQuantity,decimal AvailableQuantity,decimal ShortageQuantity);
public sealed record LowStockAlertQuery(int PageNumber=1,int PageSize=20,bool? IsActive=null,Guid? ProductId=null,Guid? WarehouseId=null);
public sealed record LowStockAlertResponse(Guid Id,Guid LowStockThresholdId,Guid ProductId,string ProductSku,Guid WarehouseId,string WarehouseCode,Guid WarehouseLocationId,string WarehouseLocationCode,decimal ThresholdQuantity,decimal AvailableQuantity,DateTimeOffset TriggeredAtUtc,DateTimeOffset LastObservedAtUtc,DateTimeOffset? ResolvedAtUtc,bool IsActive);
public sealed record LowStockMonitoringRunResult(int EvaluatedThresholdCount,int LowStockCount,int TriggeredAlertCount,int UpdatedActiveAlertCount,int ResolvedAlertCount);
