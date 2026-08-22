using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

public sealed class InventoryWarehouseDbContext(DbContextOptions<InventoryWarehouseDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryWarehouseDbContext).Assembly);
}
