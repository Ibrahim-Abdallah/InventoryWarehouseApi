using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryWarehouseApi.Infrastructure.Persistence.Configurations;

internal sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("InventoryReservations", table =>
        {
            table.HasCheckConstraint("CK_InventoryReservations_Quantity_Positive", "CAST([Quantity] AS decimal(18,3)) > 0");
            table.HasCheckConstraint("CK_InventoryReservations_Status_Supported", "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_InventoryReservations_ReferencePair",
                "([ReferenceType] IS NULL AND [ReferenceId] IS NULL) OR ([ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL)");
            table.HasCheckConstraint("CK_InventoryReservations_LifecycleConsistency", """
                ([Status] = 1 AND [ReleasedAtUtc] IS NULL AND [FulfilledAtUtc] IS NULL AND [FulfillmentMovementId] IS NULL) OR
                ([Status] = 2 AND [ReleasedAtUtc] IS NOT NULL AND [ReleasedAtUtc] >= [CreatedAtUtc] AND [FulfilledAtUtc] IS NULL AND [FulfillmentMovementId] IS NULL) OR
                ([Status] = 3 AND [ReleasedAtUtc] IS NULL AND [FulfilledAtUtc] IS NOT NULL AND [FulfilledAtUtc] >= [CreatedAtUtc] AND [FulfillmentMovementId] IS NOT NULL)
                """);
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ReferenceType).HasMaxLength(InventoryReservation.ReferenceTypeMaxLength);
        builder.Property(x => x.ReferenceId).HasMaxLength(InventoryReservation.ReferenceIdMaxLength);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WarehouseLocation>().WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.WarehouseLocationId })
            .HasPrincipalKey(x => new { x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.FulfillmentMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.FulfillmentMovementId).IsUnique().HasFilter("[FulfillmentMovementId] IS NOT NULL");
        builder.HasIndex(x => new { x.CreatedAtUtc, x.Id }).IsDescending(true, true)
            .HasDatabaseName("IX_InventoryReservations_CreatedAtUtc_Id");
        builder.HasIndex(x => new { x.ProductId, x.WarehouseId, x.WarehouseLocationId, x.CreatedAtUtc, x.Id })
            .IsDescending(false, false, false, true, true).HasDatabaseName("IX_InventoryReservations_Position_CreatedAtUtc_Id");
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
    }
}
