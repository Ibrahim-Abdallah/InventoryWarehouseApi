namespace InventoryWarehouseApi.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public abstract record PagedQuery(int PageNumber = 1, int PageSize = 20, string? Search = null,
    bool? IsActive = null, string? SortBy = null, string SortDirection = "asc");
