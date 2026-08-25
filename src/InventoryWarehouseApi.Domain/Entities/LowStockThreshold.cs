namespace InventoryWarehouseApi.Domain.Entities;

public sealed class LowStockThreshold
{
    private LowStockThreshold() { }
    public LowStockThreshold(Guid id, Guid productId, Guid warehouseId, Guid warehouseLocationId, decimal thresholdQuantity, bool isEnabled, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Threshold ID is required.", nameof(id));
        if (productId == Guid.Empty) throw new ArgumentException("Product is required.", nameof(productId));
        if (warehouseId == Guid.Empty) throw new ArgumentException("Warehouse is required.", nameof(warehouseId));
        if (warehouseLocationId == Guid.Empty) throw new ArgumentException("Warehouse location is required.", nameof(warehouseLocationId));
        ValidateQuantity(thresholdQuantity);
        Id=id; ProductId=productId; WarehouseId=warehouseId; WarehouseLocationId=warehouseLocationId;
        ThresholdQuantity=thresholdQuantity; IsEnabled=isEnabled; CreatedAtUtc=createdAtUtc.ToUniversalTime(); UpdatedAtUtc=CreatedAtUtc;
    }
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public decimal ThresholdQuantity { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public void Update(decimal quantity, bool isEnabled, DateTimeOffset updatedAtUtc)
    {
        ValidateQuantity(quantity); updatedAtUtc=updatedAtUtc.ToUniversalTime();
        if (updatedAtUtc < CreatedAtUtc) throw new ArgumentOutOfRangeException(nameof(updatedAtUtc));
        ThresholdQuantity=quantity; IsEnabled=isEnabled; UpdatedAtUtc=updatedAtUtc;
    }
    private static void ValidateQuantity(decimal value)
    { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); if (decimal.Round(value,3)!=value) throw new ArgumentException("Quantity cannot have more than 3 decimal places.", nameof(value)); }
}
