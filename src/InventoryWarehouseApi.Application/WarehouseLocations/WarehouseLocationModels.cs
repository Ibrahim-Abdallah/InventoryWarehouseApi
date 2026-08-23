using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.WarehouseLocations;

public sealed record CreateWarehouseLocationRequest(string Code, string Name, string? Description);
public sealed record UpdateWarehouseLocationRequest(string Code, string Name, string? Description, bool IsActive);
public sealed record WarehouseLocationResponse(Guid Id, Guid WarehouseId, string Code, string Name, string? Description,
    bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record WarehouseLocationQuery(int PageNumber = 1, int PageSize = 20, string? Search = null,
    bool? IsActive = null, string? SortBy = null, string SortDirection = "asc")
    : PagedQuery(PageNumber, PageSize, Search, IsActive, SortBy, SortDirection);
