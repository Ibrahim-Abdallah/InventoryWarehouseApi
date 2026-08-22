using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.Products;

public sealed record CreateProductRequest(string Sku, string Name, string? Description);
public sealed record UpdateProductRequest(string Sku, string Name, string? Description, bool IsActive);
public sealed record ProductResponse(Guid Id, string Sku, string Name, string? Description, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record ProductQuery(int PageNumber = 1, int PageSize = 20, string? Search = null,
    bool? IsActive = null, string? SortBy = null, string SortDirection = "asc")
    : PagedQuery(PageNumber, PageSize, Search, IsActive, SortBy, SortDirection);
