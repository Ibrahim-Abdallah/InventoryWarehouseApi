using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Domain.Entities;

public sealed class InventoryAdjustment
{
    public const int ReasonMaxLength = 500;
    public const int AdjustedByMaxLength = 128;

    private InventoryAdjustment() { }

    public InventoryAdjustment(Guid id, Guid productId, Guid warehouseId, Guid warehouseLocationId,
        InventoryAdjustmentType adjustmentType, decimal quantity, string reason, string adjustedBy,
        Guid stockMovementId, DateTimeOffset adjustedAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Adjustment ID is required.", nameof(id));
        if (productId == Guid.Empty) throw new ArgumentException("Product is required.", nameof(productId));
        if (warehouseId == Guid.Empty) throw new ArgumentException("Warehouse is required.", nameof(warehouseId));
        if (warehouseLocationId == Guid.Empty) throw new ArgumentException("Warehouse location is required.", nameof(warehouseLocationId));
        if (stockMovementId == Guid.Empty) throw new ArgumentException("Stock movement is required.", nameof(stockMovementId));
        if (!Enum.IsDefined(adjustmentType)) throw new ArgumentOutOfRangeException(nameof(adjustmentType), "Adjustment type is not supported.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        reason = NormalizeRequired(reason, nameof(reason));
        adjustedBy = NormalizeRequired(adjustedBy, nameof(adjustedBy));
        if (reason.Length > ReasonMaxLength) throw new ArgumentException($"Reason cannot exceed {ReasonMaxLength} characters.", nameof(reason));
        if (adjustedBy.Length > AdjustedByMaxLength) throw new ArgumentException($"Adjusted by cannot exceed {AdjustedByMaxLength} characters.", nameof(adjustedBy));

        Id = id;
        ProductId = productId;
        WarehouseId = warehouseId;
        WarehouseLocationId = warehouseLocationId;
        AdjustmentType = adjustmentType;
        Quantity = quantity;
        Reason = reason;
        AdjustedBy = adjustedBy;
        StockMovementId = stockMovementId;
        AdjustedAtUtc = adjustedAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public InventoryAdjustmentType AdjustmentType { get; private set; }
    public decimal Quantity { get; private set; }
    public string Reason { get; private set; } = null!;
    public string AdjustedBy { get; private set; } = null!;
    public Guid StockMovementId { get; private set; }
    public DateTimeOffset AdjustedAtUtc { get; private set; }

    private static string NormalizeRequired(string? value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();
}
