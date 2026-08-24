using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Domain.Entities;

public sealed class WarehouseTransfer
{
    private WarehouseTransfer() { }

    public WarehouseTransfer(Guid id, Guid sourceWarehouseId, Guid sourceWarehouseLocationId,
        Guid destinationWarehouseId, Guid destinationWarehouseLocationId, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Transfer ID is required.", nameof(id));
        if (sourceWarehouseId == Guid.Empty) throw new ArgumentException("Source warehouse is required.", nameof(sourceWarehouseId));
        if (sourceWarehouseLocationId == Guid.Empty) throw new ArgumentException("Source location is required.", nameof(sourceWarehouseLocationId));
        if (destinationWarehouseId == Guid.Empty) throw new ArgumentException("Destination warehouse is required.", nameof(destinationWarehouseId));
        if (destinationWarehouseLocationId == Guid.Empty) throw new ArgumentException("Destination location is required.", nameof(destinationWarehouseLocationId));
        if (sourceWarehouseId == destinationWarehouseId && sourceWarehouseLocationId == destinationWarehouseLocationId)
            throw new ArgumentException("Source and destination positions must differ.");
        Id = id;
        SourceWarehouseId = sourceWarehouseId;
        SourceWarehouseLocationId = sourceWarehouseLocationId;
        DestinationWarehouseId = destinationWarehouseId;
        DestinationWarehouseLocationId = destinationWarehouseLocationId;
        Status = WarehouseTransferStatus.Pending;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid SourceWarehouseId { get; private set; }
    public Guid SourceWarehouseLocationId { get; private set; }
    public Guid DestinationWarehouseId { get; private set; }
    public Guid DestinationWarehouseLocationId { get; private set; }
    public WarehouseTransferStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public ICollection<WarehouseTransferItem> Items { get; private set; } = new List<WarehouseTransferItem>();

    public void Complete(DateTimeOffset completedAtUtc)
    {
        if (Status != WarehouseTransferStatus.Pending)
            throw new InvalidOperationException("Warehouse transfer is already completed.");
        Status = WarehouseTransferStatus.Completed;
        CompletedAtUtc = completedAtUtc.ToUniversalTime();
    }
}
