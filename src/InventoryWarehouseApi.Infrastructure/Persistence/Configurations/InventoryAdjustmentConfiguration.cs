using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryWarehouseApi.Infrastructure.Persistence.Configurations;

internal sealed class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable("InventoryAdjustments", table =>
        {
            table.HasCheckConstraint("CK_InventoryAdjustments_Quantity_Positive", "CAST([Quantity] AS decimal(18,3)) > 0");
            table.HasCheckConstraint("CK_InventoryAdjustments_AdjustmentType_Supported", "[AdjustmentType] IN (1, 2)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AdjustmentType).HasConversion<int>();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.Reason).HasMaxLength(InventoryAdjustment.ReasonMaxLength).IsRequired();
        builder.Property(x => x.AdjustedBy).HasMaxLength(InventoryAdjustment.AdjustedByMaxLength).IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WarehouseLocation>().WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.WarehouseLocationId })
            .HasPrincipalKey(x => new { x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithOne().HasForeignKey<InventoryAdjustment>(x => x.StockMovementId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.StockMovementId).IsUnique();
        builder.HasIndex(x => new { x.ProductId, x.WarehouseId, x.WarehouseLocationId, x.AdjustedAtUtc, x.Id })
            .IsDescending(false, false, false, true, true)
            .HasDatabaseName("IX_InventoryAdjustments_Position_AdjustedAtUtc");
    }
}
