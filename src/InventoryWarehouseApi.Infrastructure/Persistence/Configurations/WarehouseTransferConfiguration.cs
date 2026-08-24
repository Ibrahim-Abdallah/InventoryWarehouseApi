using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryWarehouseApi.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseTransferConfiguration : IEntityTypeConfiguration<WarehouseTransfer>
{
    public void Configure(EntityTypeBuilder<WarehouseTransfer> builder)
    {
        builder.ToTable("WarehouseTransfers", table =>
        {
            table.HasCheckConstraint("CK_WarehouseTransfers_Status_Supported", "[Status] IN (1, 2)");
            table.HasCheckConstraint("CK_WarehouseTransfers_DifferentPositions",
                "[SourceWarehouseId] <> [DestinationWarehouseId] OR [SourceWarehouseLocationId] <> [DestinationWarehouseLocationId]");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.SourceWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WarehouseLocation>().WithMany()
            .HasForeignKey(x => new { x.SourceWarehouseId, x.SourceWarehouseLocationId })
            .HasPrincipalKey(x => new { x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WarehouseLocation>().WithMany()
            .HasForeignKey(x => new { x.DestinationWarehouseId, x.DestinationWarehouseLocationId })
            .HasPrincipalKey(x => new { x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.WarehouseTransferId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CreatedAtUtc, x.Id }).IsDescending(true, true)
            .HasDatabaseName("IX_WarehouseTransfers_CreatedAtUtc_Id");
        builder.HasIndex(x => new { x.SourceWarehouseId, x.SourceWarehouseLocationId });
        builder.HasIndex(x => new { x.DestinationWarehouseId, x.DestinationWarehouseLocationId });
    }
}
