using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Warehouses;
using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

internal sealed class WarehouseRepository(InventoryWarehouseDbContext dbContext) : IWarehouseRepository
{
    public Task<Warehouse?> GetAsync(Guid id, bool tracking, CancellationToken ct) =>
        (tracking ? dbContext.Warehouses : dbContext.Warehouses.AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> CodeExistsAsync(string code, Guid? excludingId, CancellationToken ct) =>
        dbContext.Warehouses.AnyAsync(x => x.Code == code && (!excludingId.HasValue || x.Id != excludingId), ct);
    public Task<bool> HasLocationsAsync(Guid id, CancellationToken ct) => dbContext.WarehouseLocations.AnyAsync(x => x.WarehouseId == id, ct);
    public async Task<PagedResult<Warehouse>> ListAsync(WarehouseQuery q, CancellationToken ct)
    {
        IQueryable<Warehouse> query = dbContext.Warehouses.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            string search = q.Search.Trim();
            query = query.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        }
        if (q.IsActive.HasValue) query = query.Where(x => x.IsActive == q.IsActive);
        int count = await query.CountAsync(ct);
        bool desc = q.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        query = (q.SortBy?.ToLowerInvariant(), desc) switch
        {
            ("code", false) => query.OrderBy(x => x.Code).ThenBy(x => x.Id), ("code", true) => query.OrderByDescending(x => x.Code).ThenBy(x => x.Id),
            ("name", false) => query.OrderBy(x => x.Name).ThenBy(x => x.Id), ("name", true) => query.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
            ("updatedatutc", false) => query.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Id), ("updatedatutc", true) => query.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Id),
            ("createdatutc", false) => query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
        };
        return new(await query.Skip((q.PageNumber - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct), q.PageNumber, q.PageSize, count);
    }
    public Task AddAsync(Warehouse warehouse, CancellationToken ct) => dbContext.Warehouses.AddAsync(warehouse, ct).AsTask();
    public void Remove(Warehouse warehouse) => dbContext.Warehouses.Remove(warehouse);
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try { await dbContext.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (UniqueConstraintDetector.Matches(ex, "UX_Warehouses_Code", "Warehouses.Code"))
        { throw new ConflictException("A warehouse with this code already exists."); }
        catch (DbUpdateException ex) when (UniqueConstraintDetector.Matches(ex, "FK_WarehouseLocations_Warehouses", "FOREIGN KEY constraint failed"))
        { throw new ConflictException("The warehouse cannot be deleted because it has warehouse locations."); }
    }
}
