using System.Net;
using System.Net.Http.Json;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Reporting;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Infrastructure.Security;
using System.Net.Http.Headers;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase10ReportingTests
{
    [Fact]
    public async Task InventoryAndWarehouseReports_AggregateIncludeZeroAndRemainReadOnly()
    {
        using ApiFactory f=new();using var client=f.CreateClient();var d=await Seed(f);
        int before;using(var s=f.Services.CreateScope())before=await s.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>().StockMovements.CountAsync();
        var summary=await client.GetFromJsonAsync<PagedResult<InventorySummaryItem>>("/api/reports/inventory-summary?sortBy=onHand&sortDirection=desc");
        Assert.Equal(2,summary!.TotalCount);var stocked=summary.Items.Single(x=>x.ProductId==d.ProductId);Assert.Equal((15m,3m,12m,2,2),(stocked.OnHandQuantity,stocked.ReservedQuantity,stocked.AvailableQuantity,stocked.WarehouseCount,stocked.LocationCount));
        var zero=summary.Items.Single(x=>x.ProductId==d.EmptyProductId);Assert.Equal((0m,0m,0m,0,0),(zero.OnHandQuantity,zero.ReservedQuantity,zero.AvailableQuantity,zero.WarehouseCount,zero.LocationCount));
        var warehouse=await client.GetFromJsonAsync<PagedResult<WarehouseInventoryItem>>($"/api/reports/warehouses/{d.WarehouseId}/inventory?search=report");Assert.Single(warehouse!.Items);Assert.Equal((10m,3m,7m,1),(warehouse.Items[0].OnHandQuantity,warehouse.Items[0].ReservedQuantity,warehouse.Items[0].AvailableQuantity,warehouse.Items[0].LocationCount));
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/reports/warehouses/{Guid.NewGuid()}/inventory")).StatusCode);
        using(var s=f.Services.CreateScope())Assert.Equal(before,await s.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>().StockMovements.CountAsync());
    }

    [Fact]
    public async Task MovementAndHistoryReports_FilterDatesAndMapSignedQuantities()
    {
        using ApiFactory f=new();using var client=f.CreateClient();var d=await Seed(f);string from=Uri.EscapeDataString("2026-01-01T00:00:00Z"),to=Uri.EscapeDataString("2026-01-02T00:00:00Z");
        var moves=await client.GetFromJsonAsync<PagedResult<StockMovementReportItem>>($"/api/reports/stock-movements?productId={d.ProductId}&fromUtc={from}&toUtc={to}&sortBy=occurredAt&sortDirection=asc");Assert.Equal(6,moves!.TotalCount);Assert.All(moves.Items,x=>Assert.Equal("REPORT-1",x.ProductSku));
        var history=await client.GetFromJsonAsync<PagedResult<ProductStockHistoryItem>>($"/api/reports/products/{d.ProductId}/stock-history?sortDirection=asc");Assert.Equal([1m,-2m,3m,-4m,-5m,6m],history!.Items.Select(x=>x.QuantityChange));
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/reports/inventory-summary?sortBy=sku%3B%20DROP%20TABLE%20Products")).StatusCode);
        var safe=await client.GetFromJsonAsync<PagedResult<InventorySummaryItem>>("/api/reports/inventory-summary?search=%27%20OR%201%3D1--");Assert.Equal(0,safe!.TotalCount);
    }

    [Fact]
    public async Task LowStockReport_MatchesInclusiveMissingBalanceAndReservedSemantics()
    {
        using ApiFactory f=new();using var client=f.CreateClient();var d=await Seed(f);
        var report=await client.GetFromJsonAsync<PagedResult<LowStockReportItem>>("/api/reports/low-stock?sortBy=shortage&sortDirection=desc");Assert.Equal(2,report!.TotalCount);
        Assert.Contains(report.Items,x=>x.ProductId==d.ProductId&&x.AvailableQuantity==7&&x.ThresholdQuantity==7&&x.ShortageQuantity==0);
        Assert.Contains(report.Items,x=>x.ProductId==d.EmptyProductId&&x.AvailableQuantity==0&&x.ShortageQuantity==4);
    }

    [Fact]
    public async Task Reports_RequireAuthentication()
    { using ApiFactory f=new(true);using var client=f.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/reports/inventory-summary")).StatusCode);using(var s=f.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();var hashing=s.ServiceProvider.GetRequiredService<IPasswordHashService>();var now=DateTimeOffset.UtcNow;var draft=new User(Guid.NewGuid(),"viewer@reports.local","Viewer","pending",UserRole.Viewer,now);db.Users.Add(new User(draft.Id,"viewer@reports.local","Viewer",hashing.HashPassword(draft,"StrongPassword1!"),UserRole.Viewer,now));await db.SaveChangesAsync();}var login=await (await client.PostAsJsonAsync("/api/auth/login",new LoginRequest("viewer@reports.local","StrongPassword1!"))).Content.ReadFromJsonAsync<AuthTokenResponse>();client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",login!.AccessToken);Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/reports/inventory-summary")).StatusCode); }

    [Fact]
    public void ReportingIndexes_ArePresentWithExistingPositionIndex()
    { using ApiFactory f=new();using var s=f.Services.CreateScope();var model=s.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>().Model;var movement=model.FindEntityType(typeof(StockMovement))!;var balance=model.FindEntityType(typeof(InventoryBalance))!;string[] names=movement.GetIndexes().Select(x=>x.GetDatabaseName()).OfType<string>().ToArray();Assert.Contains("IX_StockMovements_Position_OccurredAtUtc",names);Assert.Contains("IX_StockMovements_OccurredAtUtc_Id",names);Assert.Contains("IX_StockMovements_ProductId_OccurredAtUtc_Id",names);Assert.Contains("IX_StockMovements_WarehouseId_OccurredAtUtc_Id",names);Assert.Contains("IX_InventoryBalances_WarehouseId_ProductId_WarehouseLocationId",balance.GetIndexes().Select(x=>x.GetDatabaseName())); }

    private static async Task<(Guid ProductId,Guid EmptyProductId,Guid WarehouseId)> Seed(ApiFactory f)
    {
        using var s=f.Services.CreateScope();var db=s.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();if(await db.Products.AnyAsync())throw new InvalidOperationException();var now=new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero);
        var p=new Product("REPORT-1","Report Product",null,now);var empty=new Product("EMPTY-1","Empty Product",null,now);var w1=new Warehouse("W-1","First",null,now);var w2=new Warehouse("W-2","Second",null,now);var l1=new WarehouseLocation(w1.Id,"L-1","One",null,now);var l2=new WarehouseLocation(w2.Id,"L-2","Two",null,now);db.AddRange(p,empty,w1,w2,l1,l2);db.AddRange(new InventoryBalance(p.Id,w1.Id,l1.Id,10,3),new InventoryBalance(p.Id,w2.Id,l2.Id,5,0));
        StockMovementType[] types=Enum.GetValues<StockMovementType>();for(int i=0;i<types.Length;i++)db.StockMovements.Add(new StockMovement(p.Id,w1.Id,l1.Id,types[i],i+1,"test",$"r{i}",now.AddHours(i)));
        db.LowStockThresholds.AddRange(new LowStockThreshold(Guid.NewGuid(),p.Id,w1.Id,l1.Id,7,true,now),new LowStockThreshold(Guid.NewGuid(),empty.Id,w1.Id,l1.Id,4,true,now),new LowStockThreshold(Guid.NewGuid(),p.Id,w2.Id,l2.Id,1,false,now));await db.SaveChangesAsync();return(p.Id,empty.Id,w1.Id);
    }
}
