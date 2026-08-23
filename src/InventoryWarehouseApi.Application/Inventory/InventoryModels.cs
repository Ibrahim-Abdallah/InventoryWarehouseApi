using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Application.Inventory;

public sealed record WarehouseInventoryResponse(Guid ProductId, Guid WarehouseId, decimal OnHandQuantity,
    decimal ReservedQuantity, decimal AvailableQuantity);
public sealed record LocationInventoryResponse(Guid ProductId, Guid WarehouseId, Guid WarehouseLocationId,
    string LocationCode, string LocationName, bool IsLocationActive, decimal OnHandQuantity,
    decimal ReservedQuantity, decimal AvailableQuantity);
public sealed record LocationInventoryQuery(int PageNumber = 1, int PageSize = 20, string? Search = null,
    bool? IsActive = null, string? SortBy = null, string SortDirection = "asc")
    : PagedQuery(PageNumber, PageSize, Search, IsActive, SortBy, SortDirection);

public sealed record StockMovementRequest(decimal Quantity, string? ReferenceType = null, string? ReferenceId = null);
public sealed record StockMovementOperationResponse(Guid MovementId, StockMovementType MovementType,
    Guid ProductId, Guid WarehouseId, Guid WarehouseLocationId, decimal Quantity,
    string? ReferenceType, string? ReferenceId, DateTimeOffset OccurredAtUtc,
    decimal OnHandQuantity, decimal ReservedQuantity, decimal AvailableQuantity);
public sealed record StockMovementResponse(Guid Id, Guid ProductId, Guid WarehouseId,
    Guid WarehouseLocationId, StockMovementType MovementType, decimal Quantity,
    string? ReferenceType, string? ReferenceId, DateTimeOffset OccurredAtUtc);
public sealed record StockMovementHistoryQuery(int PageNumber = 1, int PageSize = 20);
