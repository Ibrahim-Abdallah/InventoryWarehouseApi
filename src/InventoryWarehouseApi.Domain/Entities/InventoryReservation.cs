using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Domain.Entities;

public sealed class InventoryReservation
{
    public const int ReferenceTypeMaxLength = 64;
    public const int ReferenceIdMaxLength = 128;

    private InventoryReservation() { }

    public InventoryReservation(Guid id, Guid productId, Guid warehouseId, Guid warehouseLocationId,
        decimal quantity, string? referenceType, string? referenceId, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Reservation ID is required.", nameof(id));
        if (productId == Guid.Empty) throw new ArgumentException("Product is required.", nameof(productId));
        if (warehouseId == Guid.Empty) throw new ArgumentException("Warehouse is required.", nameof(warehouseId));
        if (warehouseLocationId == Guid.Empty) throw new ArgumentException("Warehouse location is required.", nameof(warehouseLocationId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        referenceType = Normalize(referenceType);
        referenceId = Normalize(referenceId);
        if ((referenceType is null) != (referenceId is null))
            throw new ArgumentException("Reference type and reference ID must both be supplied or both be omitted.");
        if (referenceType?.Length > ReferenceTypeMaxLength)
            throw new ArgumentException($"Reference type cannot exceed {ReferenceTypeMaxLength} characters.", nameof(referenceType));
        if (referenceId?.Length > ReferenceIdMaxLength)
            throw new ArgumentException($"Reference ID cannot exceed {ReferenceIdMaxLength} characters.", nameof(referenceId));

        Id = id; ProductId = productId; WarehouseId = warehouseId; WarehouseLocationId = warehouseLocationId;
        Quantity = quantity; ReferenceType = referenceType; ReferenceId = referenceId;
        CreatedAtUtc = createdAtUtc.ToUniversalTime(); Status = InventoryReservationStatus.Active;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public decimal Quantity { get; private set; }
    public InventoryReservationStatus Status { get; private set; }
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReleasedAtUtc { get; private set; }
    public DateTimeOffset? FulfilledAtUtc { get; private set; }
    public Guid? FulfillmentMovementId { get; private set; }

    public void Release(DateTimeOffset timestamp)
    {
        EnsureActive();
        timestamp = timestamp.ToUniversalTime();
        if (timestamp < CreatedAtUtc) throw new ArgumentOutOfRangeException(nameof(timestamp), "Release timestamp cannot precede creation.");
        Status = InventoryReservationStatus.Released; ReleasedAtUtc = timestamp;
    }

    public void Fulfill(DateTimeOffset timestamp, Guid movementId)
    {
        EnsureActive();
        if (movementId == Guid.Empty) throw new ArgumentException("Movement ID is required.", nameof(movementId));
        timestamp = timestamp.ToUniversalTime();
        if (timestamp < CreatedAtUtc) throw new ArgumentOutOfRangeException(nameof(timestamp), "Fulfillment timestamp cannot precede creation.");
        Status = InventoryReservationStatus.Fulfilled; FulfilledAtUtc = timestamp; FulfillmentMovementId = movementId;
    }

    private void EnsureActive()
    {
        if (Status != InventoryReservationStatus.Active)
            throw new InvalidOperationException("Only active reservations can transition.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
