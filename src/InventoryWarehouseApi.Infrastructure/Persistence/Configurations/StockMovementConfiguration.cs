using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryWarehouseApi.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", table =>
        {
            table.HasCheckConstraint("CK_StockMovements_Quantity_Positive", "CAST([Quantity] AS decimal(18,3)) > 0");
            table.HasCheckConstraint("CK_StockMovements_MovementType_Supported", "[MovementType] IN (1, 2)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MovementType).HasConversion<int>();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.ReferenceType).HasMaxLength(StockMovement.ReferenceTypeMaxLength);
        builder.Property(x => x.ReferenceId).HasMaxLength(StockMovement.ReferenceIdMaxLength);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WarehouseLocation>().WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.WarehouseLocationId })
            .HasPrincipalKey(x => new { x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ProductId, x.WarehouseId, x.WarehouseLocationId, x.OccurredAtUtc, x.Id })
            .IsDescending(false, false, false, true, true)
            .HasDatabaseName("IX_StockMovements_Position_OccurredAtUtc");
    }
}
