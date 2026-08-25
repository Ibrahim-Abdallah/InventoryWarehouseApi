namespace InventoryWarehouseApi.Api.BackgroundJobs;
public sealed class LowStockMonitoringOptions { public const string SectionName="LowStockMonitoring"; public bool Enabled{get;set;} public int IntervalSeconds{get;set;}=60; }
