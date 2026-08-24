using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Application.WarehouseTransfers;

public sealed record CreateWarehouseTransferItemRequest(Guid ProductId, decimal Quantity);
public sealed record CreateWarehouseTransferRequest(Guid SourceWarehouseId, Guid SourceWarehouseLocationId,
    Guid DestinationWarehouseId, Guid DestinationWarehouseLocationId,
    IReadOnlyList<CreateWarehouseTransferItemRequest> Items);
public sealed record WarehouseTransferItemResponse(Guid Id, Guid ProductId, decimal Quantity,
    Guid? TransferOutMovementId, Guid? TransferInMovementId);
public sealed record WarehouseTransferResponse(Guid Id, Guid SourceWarehouseId, Guid SourceWarehouseLocationId,
    Guid DestinationWarehouseId, Guid DestinationWarehouseLocationId, WarehouseTransferStatus Status,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? CompletedAtUtc, IReadOnlyList<WarehouseTransferItemResponse> Items);
public sealed record WarehouseTransferSummaryResponse(Guid Id, Guid SourceWarehouseId, Guid SourceWarehouseLocationId,
    Guid DestinationWarehouseId, Guid DestinationWarehouseLocationId, WarehouseTransferStatus Status,
    int ItemCount, DateTimeOffset CreatedAtUtc, DateTimeOffset? CompletedAtUtc);
public sealed record WarehouseTransferHistoryQuery(int PageNumber = 1, int PageSize = 20);
