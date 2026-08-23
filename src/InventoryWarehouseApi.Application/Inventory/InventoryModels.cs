using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.Inventory;

public sealed record WarehouseInventoryResponse(Guid ProductId, Guid WarehouseId, decimal OnHandQuantity,
    decimal ReservedQuantity, decimal AvailableQuantity);
public sealed record LocationInventoryResponse(Guid ProductId, Guid WarehouseId, Guid WarehouseLocationId,
    string LocationCode, string LocationName, bool IsLocationActive, decimal OnHandQuantity,
    decimal ReservedQuantity, decimal AvailableQuantity);
public sealed record LocationInventoryQuery(int PageNumber = 1, int PageSize = 20, string? Search = null,
    bool? IsActive = null, string? SortBy = null, string SortDirection = "asc")
    : PagedQuery(PageNumber, PageSize, Search, IsActive, SortBy, SortDirection);
