using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

public sealed class InventoryWarehouseDbContext(DbContextOptions<InventoryWarehouseDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();
    public DbSet<WarehouseTransfer> WarehouseTransfers => Set<WarehouseTransfer>();
    public DbSet<WarehouseTransferItem> WarehouseTransferItems => Set<WarehouseTransferItem>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<LowStockThreshold> LowStockThresholds => Set<LowStockThreshold>();
    public DbSet<LowStockAlert> LowStockAlerts => Set<LowStockAlert>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryWarehouseDbContext).Assembly);
}
