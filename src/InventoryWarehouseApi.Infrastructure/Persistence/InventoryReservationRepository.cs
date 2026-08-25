using System.Data;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.InventoryReservations;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

internal sealed class InventoryReservationRepository(InventoryWarehouseDbContext dbContext) : IInventoryReservationRepository
{
    public async Task<InventoryReservation> CreateAsync(CreateInventoryReservationRequest request, DateTimeOffset timestamp, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await dbContext.Products.AnyAsync(x => x.Id == request.ProductId, ct)) throw new NotFoundException("Product was not found.");
        if (!await dbContext.Warehouses.AnyAsync(x => x.Id == request.WarehouseId, ct)) throw new NotFoundException("Warehouse was not found.");
        if (!await dbContext.WarehouseLocations.AnyAsync(x => x.WarehouseId == request.WarehouseId && x.Id == request.WarehouseLocationId, ct))
            throw new NotFoundException("Warehouse location was not found.");
        InventoryBalance balance = await FindBalanceAsync(request.ProductId, request.WarehouseId, request.WarehouseLocationId, ct)
            ?? throw new ConflictException("No inventory balance exists for this reservation.");
        if (request.Quantity > balance.AvailableQuantity) throw new ConflictException("Insufficient available stock for reservation.");
        balance.Reserve(request.Quantity);
        InventoryReservation reservation = new(Guid.NewGuid(), request.ProductId, request.WarehouseId,
            request.WarehouseLocationId, request.Quantity, request.ReferenceType, request.ReferenceId, timestamp);
        dbContext.InventoryReservations.Add(reservation);
        await dbContext.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return reservation;
    }

    public Task<InventoryReservation?> GetAsync(Guid id, CancellationToken ct) =>
        dbContext.InventoryReservations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<InventoryReservation> ReleaseAsync(Guid id, DateTimeOffset timestamp, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        InventoryReservation reservation = await FindReservationAsync(id, ct);
        if (reservation.Status == InventoryReservationStatus.Released) throw new ConflictException("Inventory reservation is already released.");
        if (reservation.Status == InventoryReservationStatus.Fulfilled) throw new ConflictException("Fulfilled inventory reservation cannot be released.");
        InventoryBalance balance = await FindBalanceAsync(reservation.ProductId, reservation.WarehouseId, reservation.WarehouseLocationId, ct)
            ?? throw new ConflictException("Inventory balance required by the reservation no longer exists.");
        if (reservation.Quantity > balance.ReservedQuantity) throw new ConflictException("Reserved inventory is insufficient to release this reservation.");
        balance.ReleaseReservation(reservation.Quantity); reservation.Release(timestamp);
        await dbContext.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return reservation;
    }

    public async Task<InventoryReservation> FulfillAsync(Guid id, DateTimeOffset timestamp, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        InventoryReservation reservation = await FindReservationAsync(id, ct);
        if (reservation.Status == InventoryReservationStatus.Fulfilled) throw new ConflictException("Inventory reservation is already fulfilled.");
        if (reservation.Status == InventoryReservationStatus.Released) throw new ConflictException("Released inventory reservation cannot be fulfilled.");
        InventoryBalance balance = await FindBalanceAsync(reservation.ProductId, reservation.WarehouseId, reservation.WarehouseLocationId, ct)
            ?? throw new ConflictException("Inventory balance required by the reservation no longer exists.");
        if (reservation.Quantity > balance.ReservedQuantity) throw new ConflictException("Reserved inventory is insufficient to fulfill this reservation.");
        timestamp = timestamp.ToUniversalTime(); Guid movementId = Guid.NewGuid();
        balance.FulfillReservation(reservation.Quantity);
        dbContext.StockMovements.Add(new StockMovement(movementId, reservation.ProductId, reservation.WarehouseId,
            reservation.WarehouseLocationId, StockMovementType.StockOut, reservation.Quantity,
            "InventoryReservation", reservation.Id.ToString("D"), timestamp));
        reservation.Fulfill(timestamp, movementId);
        await dbContext.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return reservation;
    }

    public async Task<PagedResult<InventoryReservation>> ListAsync(InventoryReservationHistoryQuery query, CancellationToken ct)
    {
        int count = await dbContext.InventoryReservations.CountAsync(ct);
        int offset = (query.PageNumber - 1) * query.PageSize;
        List<InventoryReservation> items = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
            ? await dbContext.InventoryReservations.FromSqlInterpolated($"""SELECT * FROM InventoryReservations ORDER BY CreatedAtUtc DESC, Id DESC LIMIT {query.PageSize} OFFSET {offset}""").AsNoTracking().ToListAsync(ct)
            : await dbContext.InventoryReservations.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
                .Skip(offset).Take(query.PageSize).ToListAsync(ct);
        return new(items, query.PageNumber, query.PageSize, count);
    }

    private async Task<InventoryReservation> FindReservationAsync(Guid id, CancellationToken ct) =>
        await dbContext.InventoryReservations.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Inventory reservation was not found.");
    private Task<InventoryBalance?> FindBalanceAsync(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct) =>
        dbContext.InventoryBalances.SingleOrDefaultAsync(x => x.ProductId == productId && x.WarehouseId == warehouseId && x.WarehouseLocationId == locationId, ct);
}
