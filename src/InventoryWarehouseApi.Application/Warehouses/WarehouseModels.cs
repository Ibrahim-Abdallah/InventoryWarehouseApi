using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.Warehouses;

public sealed record CreateWarehouseRequest(string Code, string Name, string? Description);
public sealed record UpdateWarehouseRequest(string Code, string Name, string? Description, bool IsActive);
public sealed record WarehouseResponse(Guid Id, string Code, string Name, string? Description, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record WarehouseQuery(int PageNumber = 1, int PageSize = 20, string? Search = null,
    bool? IsActive = null, string? SortBy = null, string SortDirection = "asc")
    : PagedQuery(PageNumber, PageSize, Search, IsActive, SortBy, SortDirection);
