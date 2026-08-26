using System.Data.Common;
using Dapper;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Reporting;
using System.Data;
using System.Globalization;

namespace InventoryWarehouseApi.Infrastructure.Reporting;

internal sealed class DapperInventoryReportingRepository(IReportingConnectionFactory connections):IInventoryReportingRepository
{
    static DapperInventoryReportingRepository()
    {
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new DecimalHandler());
    }
    public Task<bool> ProductExistsAsync(Guid id,CancellationToken ct)=>Exists("Products",id,ct);
    public Task<bool> WarehouseExistsAsync(Guid id,CancellationToken ct)=>Exists("Warehouses",id,ct);
    private async Task<bool> Exists(string table,Guid id,CancellationToken ct){await using var c=connections.CreateConnection();return await c.ExecuteScalarAsync<int>(new CommandDefinition($"SELECT COUNT(1) FROM {table} WHERE Id=@Id",new{Id=id},cancellationToken:ct))>0;}

    public Task<PagedResult<InventorySummaryItem>> ListInventorySummaryAsync(InventorySummaryQuery q,CancellationToken ct)
    {
        const string source="""FROM Products p LEFT JOIN InventoryBalances b ON b.ProductId=p.Id WHERE (@Search IS NULL OR p.Sku LIKE @Pattern OR p.Name LIKE @Pattern) AND (@IsActive IS NULL OR p.IsActive=@IsActive)""";
        string grouped=source+" GROUP BY p.Id,p.Sku,p.Name,p.IsActive";
        string columns="p.Id ProductId,p.Sku ProductSku,p.Name ProductName,p.IsActive IsProductActive,COALESCE(SUM(b.OnHandQuantity),0) OnHandQuantity,COALESCE(SUM(b.ReservedQuantity),0) ReservedQuantity,COALESCE(SUM(b.OnHandQuantity-b.ReservedQuantity),0) AvailableQuantity,COUNT(DISTINCT b.WarehouseId) WarehouseCount,COUNT(b.WarehouseLocationId) LocationCount";
        string order=Sort(q.SortBy,q.SortDirection,new(){["sku"]="ProductSku",["name"]="ProductName",["onhand"]="OnHandQuantity",["reserved"]="ReservedQuantity",["available"]="AvailableQuantity"},"ProductId",q.SortBy?.Equals("sku",StringComparison.OrdinalIgnoreCase)==true);
        return Page<InventorySummaryItem>($"SELECT COUNT(1) FROM (SELECT p.Id {grouped}) x; SELECT {columns} {grouped} ORDER BY {order} {Paging}",Args(q,q.Search,q.IsActive),q,ct);
    }

    public Task<PagedResult<WarehouseInventoryItem>> ListWarehouseInventoryAsync(Guid warehouseId,WarehouseInventoryQuery q,CancellationToken ct)
    {
        const string source="""FROM InventoryBalances b JOIN Products p ON p.Id=b.ProductId WHERE b.WarehouseId=@WarehouseId AND (@Search IS NULL OR p.Sku LIKE @Pattern OR p.Name LIKE @Pattern) AND (@IsActive IS NULL OR p.IsActive=@IsActive)""";
        string grouped=source+" GROUP BY p.Id,p.Sku,p.Name,p.IsActive";
        string columns="p.Id ProductId,p.Sku ProductSku,p.Name ProductName,p.IsActive IsProductActive,SUM(b.OnHandQuantity) OnHandQuantity,SUM(b.ReservedQuantity) ReservedQuantity,SUM(b.OnHandQuantity-b.ReservedQuantity) AvailableQuantity,COUNT(b.WarehouseLocationId) LocationCount";
        string order=Sort(q.SortBy,q.SortDirection,new(){["sku"]="ProductSku",["name"]="ProductName",["onhand"]="OnHandQuantity",["reserved"]="ReservedQuantity",["available"]="AvailableQuantity",["locations"]="LocationCount"},"ProductId",q.SortBy?.Equals("sku",StringComparison.OrdinalIgnoreCase)==true);
        var a=Args(q,q.Search,q.IsActive);a.Add("WarehouseId",warehouseId);
        return Page<WarehouseInventoryItem>($"SELECT COUNT(1) FROM (SELECT p.Id {grouped}) x; SELECT {columns} {grouped} ORDER BY {order} {Paging}",a,q,ct);
    }

    public Task<PagedResult<StockMovementReportItem>> ListStockMovementsAsync(StockMovementReportQuery q,CancellationToken ct)
    {
        const string source="""FROM StockMovements m JOIN Products p ON p.Id=m.ProductId JOIN Warehouses w ON w.Id=m.WarehouseId JOIN WarehouseLocations l ON l.Id=m.WarehouseLocationId AND l.WarehouseId=m.WarehouseId WHERE (@ProductId IS NULL OR m.ProductId=@ProductId) AND (@WarehouseId IS NULL OR m.WarehouseId=@WarehouseId) AND (@LocationId IS NULL OR m.WarehouseLocationId=@LocationId) AND (@MovementType IS NULL OR m.MovementType=@MovementType) AND (@FromUtc IS NULL OR m.OccurredAtUtc>=@FromUtc) AND (@ToUtc IS NULL OR m.OccurredAtUtc<@ToUtc) AND (@ReferenceType IS NULL OR m.ReferenceType=@ReferenceType) AND (@ReferenceId IS NULL OR m.ReferenceId=@ReferenceId)""";
        string columns="m.Id MovementId,m.OccurredAtUtc,m.MovementType,m.Quantity,p.Id ProductId,p.Sku ProductSku,p.Name ProductName,w.Id WarehouseId,w.Code WarehouseCode,w.Name WarehouseName,l.Id WarehouseLocationId,l.Code WarehouseLocationCode,l.Name WarehouseLocationName,m.ReferenceType,m.ReferenceId";
        string order=MovementOrder(q.SortBy,q.SortDirection);
        var a=Args(q);a.AddDynamicParams(new{q.ProductId,q.WarehouseId,LocationId=q.WarehouseLocationId,MovementType=(int?)q.MovementType,q.FromUtc,q.ToUtc,q.ReferenceType,q.ReferenceId});
        return Page<StockMovementReportItem>($"SELECT COUNT(1) {source}; SELECT {columns} {source} ORDER BY {order} {Paging}",a,q,ct);
    }

    public Task<PagedResult<LowStockReportItem>> ListLowStockAsync(LowStockReportQuery q,CancellationToken ct)
    {
        const string source="""FROM LowStockThresholds t JOIN Products p ON p.Id=t.ProductId JOIN Warehouses w ON w.Id=t.WarehouseId JOIN WarehouseLocations l ON l.Id=t.WarehouseLocationId AND l.WarehouseId=t.WarehouseId LEFT JOIN InventoryBalances b ON b.ProductId=t.ProductId AND b.WarehouseId=t.WarehouseId AND b.WarehouseLocationId=t.WarehouseLocationId WHERE t.IsEnabled=1 AND p.IsActive=1 AND w.IsActive=1 AND l.IsActive=1 AND COALESCE(b.OnHandQuantity,0)-COALESCE(b.ReservedQuantity,0)<=t.ThresholdQuantity AND (@Search IS NULL OR p.Sku LIKE @Pattern OR p.Name LIKE @Pattern) AND (@ProductId IS NULL OR t.ProductId=@ProductId) AND (@WarehouseId IS NULL OR t.WarehouseId=@WarehouseId)""";
        string columns="t.Id LowStockThresholdId,p.Id ProductId,p.Sku ProductSku,p.Name ProductName,w.Id WarehouseId,w.Code WarehouseCode,w.Name WarehouseName,l.Id WarehouseLocationId,l.Code WarehouseLocationCode,l.Name WarehouseLocationName,t.ThresholdQuantity,COALESCE(b.OnHandQuantity,0) OnHandQuantity,COALESCE(b.ReservedQuantity,0) ReservedQuantity,COALESCE(b.OnHandQuantity,0)-COALESCE(b.ReservedQuantity,0) AvailableQuantity,CASE WHEN t.ThresholdQuantity-(COALESCE(b.OnHandQuantity,0)-COALESCE(b.ReservedQuantity,0))>0 THEN t.ThresholdQuantity-(COALESCE(b.OnHandQuantity,0)-COALESCE(b.ReservedQuantity,0)) ELSE 0 END ShortageQuantity";
        string order=Sort(q.SortBy,q.SortDirection,new(){["shortage"]="ShortageQuantity",["sku"]="ProductSku",["available"]="AvailableQuantity",["threshold"]="ThresholdQuantity",["warehouse"]="WarehouseCode"},"LowStockThresholdId",false,q.SortBy?.Equals("shortage",StringComparison.OrdinalIgnoreCase)==true?"ProductSku ASC":null);
        var a=Args(q,q.Search);a.AddDynamicParams(new{q.ProductId,q.WarehouseId});
        return Page<LowStockReportItem>($"SELECT COUNT(1) {source}; SELECT {columns} {source} ORDER BY {order} {Paging}",a,q,ct);
    }

    public Task<PagedResult<ProductStockHistoryItem>> ListProductStockHistoryAsync(Guid productId,ProductStockHistoryQuery q,CancellationToken ct)
    {
        const string source="""FROM StockMovements m JOIN Products p ON p.Id=m.ProductId JOIN Warehouses w ON w.Id=m.WarehouseId JOIN WarehouseLocations l ON l.Id=m.WarehouseLocationId AND l.WarehouseId=m.WarehouseId WHERE m.ProductId=@ProductId AND (@WarehouseId IS NULL OR m.WarehouseId=@WarehouseId) AND (@LocationId IS NULL OR m.WarehouseLocationId=@LocationId) AND (@MovementType IS NULL OR m.MovementType=@MovementType) AND (@FromUtc IS NULL OR m.OccurredAtUtc>=@FromUtc) AND (@ToUtc IS NULL OR m.OccurredAtUtc<@ToUtc)""";
        string columns="m.Id MovementId,p.Id ProductId,p.Sku ProductSku,p.Name ProductName,m.OccurredAtUtc,m.MovementType,m.Quantity,CASE WHEN m.MovementType IN (1,3,6) THEN m.Quantity ELSE -m.Quantity END QuantityChange,w.Id WarehouseId,w.Code WarehouseCode,w.Name WarehouseName,l.Id WarehouseLocationId,l.Code WarehouseLocationCode,l.Name WarehouseLocationName,m.ReferenceType,m.ReferenceId";
        string direction=Direction(q.SortDirection);var a=Args(q);a.AddDynamicParams(new{ProductId=productId,q.WarehouseId,LocationId=q.WarehouseLocationId,MovementType=(int?)q.MovementType,q.FromUtc,q.ToUtc});
        return Page<ProductStockHistoryItem>($"SELECT COUNT(1) {source}; SELECT {columns} {source} ORDER BY m.OccurredAtUtc {direction},m.Id {direction} {Paging}",a,q,ct);
    }

    private string Paging=>connections.Dialect==ReportingDialect.SqlServer?"OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY":"LIMIT @PageSize OFFSET @Offset";
    private async Task<PagedResult<T>> Page<T>(string sql,DynamicParameters args,ReportPageQuery q,CancellationToken ct){await using DbConnection c=connections.CreateConnection();args.Add("Offset",(q.PageNumber-1)*q.PageSize);args.Add("PageSize",q.PageSize);using var multi=await c.QueryMultipleAsync(new CommandDefinition(sql,args,cancellationToken:ct));int count=await multi.ReadSingleAsync<int>();var rows=(await multi.ReadAsync<T>()).AsList();return new(rows,q.PageNumber,q.PageSize,count);}
    private static DynamicParameters Args(ReportPageQuery q,string? search=null,bool? active=null){var a=new DynamicParameters();a.Add("Search",search);a.Add("Pattern",search is null?null:$"%{search}%");a.Add("IsActive",active);return a;}
    private static string Direction(string d)=>d.Equals("desc",StringComparison.OrdinalIgnoreCase)?"DESC":"ASC";
    private static string Sort(string? sort,string direction,Dictionary<string,string> map,string id,bool idSameDirection,string? middle=null){string key=(sort??"").ToLowerInvariant();string field=map[key];string dir=Direction(direction);return $"{field} {dir}{(middle is null?"":","+middle)},{id} {(idSameDirection?dir:"ASC")}";}
    private static string MovementOrder(string? sort,string direction){string dir=Direction(direction);return (sort??"").ToLowerInvariant() switch{"sku"=>$"p.Sku {dir},m.Id {dir}","quantity"=>$"m.Quantity {dir},m.OccurredAtUtc DESC,m.Id DESC","movementtype"=>$"m.MovementType {dir},m.OccurredAtUtc DESC,m.Id DESC",_=>$"m.OccurredAtUtc {dir},m.Id {dir}"};}
    private sealed class GuidHandler:SqlMapper.TypeHandler<Guid>{public override Guid Parse(object value)=>value is Guid g?g:Guid.Parse(Convert.ToString(value,CultureInfo.InvariantCulture)!);public override void SetValue(IDbDataParameter parameter,Guid value)=>parameter.Value=value;}
    private sealed class DateTimeOffsetHandler:SqlMapper.TypeHandler<DateTimeOffset>{public override DateTimeOffset Parse(object value)=>value is DateTimeOffset d?d:value is DateTime dt?new DateTimeOffset(dt):DateTimeOffset.Parse(Convert.ToString(value,CultureInfo.InvariantCulture)!,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind);public override void SetValue(IDbDataParameter parameter,DateTimeOffset value)=>parameter.Value=value;}
    private sealed class DecimalHandler:SqlMapper.TypeHandler<decimal>{public override decimal Parse(object value)=>Convert.ToDecimal(value,CultureInfo.InvariantCulture);public override void SetValue(IDbDataParameter parameter,decimal value)=>parameter.Value=value;}
}
