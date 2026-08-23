using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Domain.Entities;

public sealed class StockMovement
{
    public const int ReferenceTypeMaxLength = 64;
    public const int ReferenceIdMaxLength = 128;

    private StockMovement() { }

    public StockMovement(Guid productId, Guid warehouseId, Guid warehouseLocationId,
        StockMovementType movementType, decimal quantity, string? referenceType,
        string? referenceId, DateTimeOffset occurredAtUtc)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Product is required.", nameof(productId));
        if (warehouseId == Guid.Empty) throw new ArgumentException("Warehouse is required.", nameof(warehouseId));
        if (warehouseLocationId == Guid.Empty) throw new ArgumentException("Warehouse location is required.", nameof(warehouseLocationId));
        if (movementType is not StockMovementType.StockIn and not StockMovementType.StockOut)
            throw new ArgumentOutOfRangeException(nameof(movementType), "Movement type is not supported.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        referenceType = Normalize(referenceType);
        referenceId = Normalize(referenceId);
        if ((referenceType is null) != (referenceId is null))
            throw new ArgumentException("Reference type and reference ID must both be supplied or both be omitted.");
        if (referenceType?.Length > ReferenceTypeMaxLength)
            throw new ArgumentException($"Reference type cannot exceed {ReferenceTypeMaxLength} characters.", nameof(referenceType));
        if (referenceId?.Length > ReferenceIdMaxLength)
            throw new ArgumentException($"Reference ID cannot exceed {ReferenceIdMaxLength} characters.", nameof(referenceId));

        Id = Guid.NewGuid();
        ProductId = productId;
        WarehouseId = warehouseId;
        WarehouseLocationId = warehouseLocationId;
        MovementType = movementType;
        Quantity = quantity;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public decimal Quantity { get; private set; }
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
