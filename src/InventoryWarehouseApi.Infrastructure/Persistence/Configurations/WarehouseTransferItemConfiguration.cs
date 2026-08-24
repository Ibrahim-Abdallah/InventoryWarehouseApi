using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryWarehouseApi.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseTransferItemConfiguration : IEntityTypeConfiguration<WarehouseTransferItem>
{
    public void Configure(EntityTypeBuilder<WarehouseTransferItem> builder)
    {
        builder.ToTable("WarehouseTransferItems", table =>
        {
            table.HasCheckConstraint("CK_WarehouseTransferItems_Quantity_Positive", "CAST([Quantity] AS decimal(18,3)) > 0");
            table.HasCheckConstraint("CK_WarehouseTransferItems_MovementPair",
                "([TransferOutMovementId] IS NULL AND [TransferInMovementId] IS NULL) OR ([TransferOutMovementId] IS NOT NULL AND [TransferInMovementId] IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.TransferOutMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.TransferInMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WarehouseTransferId, x.ProductId }).IsUnique()
            .HasDatabaseName("UX_WarehouseTransferItems_Transfer_Product");
        builder.HasIndex(x => x.TransferOutMovementId).IsUnique().HasFilter("[TransferOutMovementId] IS NOT NULL")
            .HasDatabaseName("UX_WarehouseTransferItems_TransferOutMovementId");
        builder.HasIndex(x => x.TransferInMovementId).IsUnique().HasFilter("[TransferInMovementId] IS NOT NULL")
            .HasDatabaseName("UX_WarehouseTransferItems_TransferInMovementId");
    }
}
