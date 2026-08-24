namespace InventoryWarehouseApi.Domain.Entities;

public sealed class WarehouseTransferItem
{
    private WarehouseTransferItem() { }

    public WarehouseTransferItem(Guid id, Guid warehouseTransferId, Guid productId, decimal quantity)
    {
        if (id == Guid.Empty) throw new ArgumentException("Transfer item ID is required.", nameof(id));
        if (warehouseTransferId == Guid.Empty) throw new ArgumentException("Warehouse transfer is required.", nameof(warehouseTransferId));
        if (productId == Guid.Empty) throw new ArgumentException("Product is required.", nameof(productId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        Id = id;
        WarehouseTransferId = warehouseTransferId;
        ProductId = productId;
        Quantity = quantity;
    }

    public Guid Id { get; private set; }
    public Guid WarehouseTransferId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid? TransferOutMovementId { get; private set; }
    public Guid? TransferInMovementId { get; private set; }

    public void AttachMovements(Guid? transferOutMovementId, Guid? transferInMovementId)
    {
        if (!transferOutMovementId.HasValue || !transferInMovementId.HasValue)
            throw new ArgumentException("Transfer-out and transfer-in movement IDs must both be supplied.");
        if (transferOutMovementId == Guid.Empty) throw new ArgumentException("Transfer-out movement ID is required.", nameof(transferOutMovementId));
        if (transferInMovementId == Guid.Empty) throw new ArgumentException("Transfer-in movement ID is required.", nameof(transferInMovementId));
        if (transferOutMovementId == transferInMovementId) throw new ArgumentException("Transfer movement IDs must differ.");
        if (TransferOutMovementId.HasValue || TransferInMovementId.HasValue)
            throw new InvalidOperationException("Transfer movement links have already been assigned.");
        TransferOutMovementId = transferOutMovementId;
        TransferInMovementId = transferInMovementId;
    }
}
