using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.WarehouseLocations;
using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

internal sealed class WarehouseLocationRepository(InventoryWarehouseDbContext dbContext) : IWarehouseLocationRepository
{
    public Task<bool> WarehouseExistsAsync(Guid id, CancellationToken ct) => dbContext.Warehouses.AnyAsync(x => x.Id == id, ct);
    public Task<WarehouseLocation?> GetAsync(Guid warehouseId, Guid id, bool tracking, CancellationToken ct) =>
        (tracking ? dbContext.WarehouseLocations : dbContext.WarehouseLocations.AsNoTracking())
            .SingleOrDefaultAsync(x => x.WarehouseId == warehouseId && x.Id == id, ct);
    public Task<bool> CodeExistsAsync(Guid warehouseId, string code, Guid? excludingId, CancellationToken ct) =>
        dbContext.WarehouseLocations.AnyAsync(x => x.WarehouseId == warehouseId && x.Code == code && (!excludingId.HasValue || x.Id != excludingId), ct);
    public Task<bool> HasInventoryAsync(Guid warehouseId, Guid id, CancellationToken ct) =>
        dbContext.InventoryBalances.AnyAsync(x => x.WarehouseId == warehouseId && x.WarehouseLocationId == id, ct);
    public Task<bool> HasMovementsAsync(Guid warehouseId, Guid id, CancellationToken ct) =>
        dbContext.StockMovements.AnyAsync(x => x.WarehouseId == warehouseId && x.WarehouseLocationId == id, ct);
    public Task<bool> HasTransfersAsync(Guid warehouseId, Guid id, CancellationToken ct) =>
        dbContext.WarehouseTransfers.AnyAsync(x =>
            (x.SourceWarehouseId == warehouseId && x.SourceWarehouseLocationId == id) ||
            (x.DestinationWarehouseId == warehouseId && x.DestinationWarehouseLocationId == id), ct);
    public Task<bool> HasReservationsAsync(Guid warehouseId, Guid id, CancellationToken ct) =>
        dbContext.InventoryReservations.AnyAsync(x => x.WarehouseId == warehouseId && x.WarehouseLocationId == id, ct);
    public async Task<PagedResult<WarehouseLocation>> ListAsync(Guid warehouseId, WarehouseLocationQuery q, CancellationToken ct)
    {
        IQueryable<WarehouseLocation> query = dbContext.WarehouseLocations.AsNoTracking().Where(x => x.WarehouseId == warehouseId);
        if (!string.IsNullOrWhiteSpace(q.Search)) { string search = q.Search.Trim(); query = query.Where(x => x.Code.Contains(search) || x.Name.Contains(search)); }
        if (q.IsActive.HasValue) query = query.Where(x => x.IsActive == q.IsActive.Value);
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
    public Task AddAsync(WarehouseLocation location, CancellationToken ct) => dbContext.WarehouseLocations.AddAsync(location, ct).AsTask();
    public void Remove(WarehouseLocation location) => dbContext.WarehouseLocations.Remove(location);
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try { await dbContext.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (UniqueConstraintDetector.Matches(ex, "UX_WarehouseLocations_WarehouseId_Code", "WarehouseLocations.WarehouseId, WarehouseLocations.Code"))
        { throw new ConflictException("A location with this code already exists in this warehouse."); }
        catch (DbUpdateException ex) when (UniqueConstraintDetector.Matches(ex, "FK_InventoryBalances_WarehouseLocations", "FOREIGN KEY constraint failed"))
        { throw new ConflictException("The warehouse location cannot be deleted because it has inventory balances."); }
        catch (DbUpdateException ex) when (UniqueConstraintDetector.Matches(ex, "FK_StockMovements_WarehouseLocations"))
        { throw new ConflictException("The warehouse location cannot be deleted because it has stock movement history."); }
        catch (DbUpdateException ex) when (UniqueConstraintDetector.Matches(ex, "FK_WarehouseTransfers_WarehouseLocations"))
        { throw new ConflictException("The warehouse location cannot be deleted because it is referenced by warehouse transfers."); }
        catch (DbUpdateException ex) when (UniqueConstraintDetector.Matches(ex, "FK_InventoryReservations_WarehouseLocations"))
        { throw new ConflictException("The warehouse location cannot be deleted because it is referenced by inventory reservations."); }
    }
}
