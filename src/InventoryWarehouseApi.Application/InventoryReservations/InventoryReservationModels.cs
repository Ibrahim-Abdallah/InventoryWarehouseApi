using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Application.InventoryReservations;

public sealed record CreateInventoryReservationRequest(Guid ProductId, Guid WarehouseId,
    Guid WarehouseLocationId, decimal Quantity, string? ReferenceType, string? ReferenceId);
public sealed record InventoryReservationResponse(Guid Id, Guid ProductId, Guid WarehouseId,
    Guid WarehouseLocationId, decimal Quantity, InventoryReservationStatus Status, string? ReferenceType,
    string? ReferenceId, DateTimeOffset CreatedAtUtc, DateTimeOffset? ReleasedAtUtc,
    DateTimeOffset? FulfilledAtUtc, Guid? FulfillmentMovementId);
public sealed record InventoryReservationHistoryQuery(int PageNumber = 1, int PageSize = 20);
