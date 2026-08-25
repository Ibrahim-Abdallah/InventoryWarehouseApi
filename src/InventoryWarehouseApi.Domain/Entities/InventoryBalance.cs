namespace InventoryWarehouseApi.Domain.Entities;

public sealed class InventoryBalance
{
    private InventoryBalance() { }

    public InventoryBalance(Guid productId, Guid warehouseId, Guid warehouseLocationId,
        decimal onHandQuantity, decimal reservedQuantity)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Product is required.", nameof(productId));
        if (warehouseId == Guid.Empty) throw new ArgumentException("Warehouse is required.", nameof(warehouseId));
        if (warehouseLocationId == Guid.Empty) throw new ArgumentException("Warehouse location is required.", nameof(warehouseLocationId));
        SetQuantities(onHandQuantity, reservedQuantity);
        ProductId = productId;
        WarehouseId = warehouseId;
        WarehouseLocationId = warehouseLocationId;
    }

    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public decimal OnHandQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal AvailableQuantity => OnHandQuantity - ReservedQuantity;

    public void Receive(decimal quantity)
    {
        EnsurePositive(quantity);
        OnHandQuantity += quantity;
    }

    public void Issue(decimal quantity)
    {
        EnsurePositive(quantity);
        if (quantity > AvailableQuantity)
            throw new InvalidOperationException("Insufficient available stock.");
        OnHandQuantity -= quantity;
    }

    public void Reserve(decimal quantity)
    {
        EnsurePositive(quantity);
        if (quantity > AvailableQuantity)
            throw new InvalidOperationException("Insufficient available stock for reservation.");
        ReservedQuantity += quantity;
    }

    public void ReleaseReservation(decimal quantity)
    {
        EnsurePositive(quantity);
        if (quantity > ReservedQuantity)
            throw new InvalidOperationException("Insufficient reserved stock.");
        ReservedQuantity -= quantity;
    }

    public void FulfillReservation(decimal quantity)
    {
        EnsurePositive(quantity);
        if (quantity > ReservedQuantity)
            throw new InvalidOperationException("Insufficient reserved stock.");
        OnHandQuantity -= quantity;
        ReservedQuantity -= quantity;
    }

    private static void EnsurePositive(decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
    }

    private void SetQuantities(decimal onHandQuantity, decimal reservedQuantity)
    {
        if (onHandQuantity < 0) throw new ArgumentOutOfRangeException(nameof(onHandQuantity), "On-hand quantity cannot be negative.");
        if (reservedQuantity < 0) throw new ArgumentOutOfRangeException(nameof(reservedQuantity), "Reserved quantity cannot be negative.");
        if (reservedQuantity > onHandQuantity) throw new ArgumentException("Reserved quantity cannot exceed on-hand quantity.", nameof(reservedQuantity));
        OnHandQuantity = onHandQuantity;
        ReservedQuantity = reservedQuantity;
    }
}
