using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryWarehouseApi.Infrastructure.Persistence.Configurations;

internal sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("InventoryBalances", table =>
        {
            table.HasCheckConstraint("CK_InventoryBalances_OnHandQuantity_NonNegative", "CAST([OnHandQuantity] AS decimal(18,3)) >= 0");
            table.HasCheckConstraint("CK_InventoryBalances_ReservedQuantity_NonNegative", "CAST([ReservedQuantity] AS decimal(18,3)) >= 0");
            table.HasCheckConstraint("CK_InventoryBalances_ReservedNotGreaterThanOnHand", "CAST([ReservedQuantity] AS decimal(18,3)) <= CAST([OnHandQuantity] AS decimal(18,3))");
        });
        builder.HasKey(x => new { x.ProductId, x.WarehouseId, x.WarehouseLocationId });
        builder.Property(x => x.OnHandQuantity).HasPrecision(18, 3);
        builder.Property(x => x.ReservedQuantity).HasPrecision(18, 3);
        builder.Ignore(x => x.AvailableQuantity);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WarehouseLocation>().WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.WarehouseLocationId })
            .HasPrincipalKey(x => new { x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ProductId, x.WarehouseId });
        builder.HasIndex(x=>new{x.WarehouseId,x.ProductId,x.WarehouseLocationId}).HasDatabaseName("IX_InventoryBalances_WarehouseId_ProductId_WarehouseLocationId");
    }
}
