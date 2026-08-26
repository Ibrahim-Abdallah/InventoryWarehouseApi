using System.Data;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.WarehouseTransfers;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

internal sealed class WarehouseTransferRepository(InventoryWarehouseDbContext dbContext) : IWarehouseTransferRepository
{
    public async Task<WarehouseTransfer> CreateAsync(CreateWarehouseTransferRequest request,
        DateTimeOffset createdAtUtc, CancellationToken ct)
    {
        await EnsurePositionAsync(request.SourceWarehouseId, request.SourceWarehouseLocationId, "Source", ct);
        await EnsurePositionAsync(request.DestinationWarehouseId, request.DestinationWarehouseLocationId, "Destination", ct);
        foreach (CreateWarehouseTransferItemRequest item in request.Items)
        {
            if (!await dbContext.Products.AnyAsync(x => x.Id == item.ProductId, ct))
                throw new NotFoundException($"Product '{item.ProductId}' was not found.");
            InventoryBalance? balance = await FindBalanceAsync(item.ProductId, request.SourceWarehouseId,
                request.SourceWarehouseLocationId, ct);
            if (balance is null)
                throw new ConflictException($"No source inventory balance exists for product '{item.ProductId}'.");
            if (item.Quantity > balance.AvailableQuantity)
                throw new ConflictException($"Insufficient available stock for product '{item.ProductId}'.");
        }

        Guid transferId = Guid.NewGuid();
        WarehouseTransfer transfer = new(transferId, request.SourceWarehouseId, request.SourceWarehouseLocationId,
            request.DestinationWarehouseId, request.DestinationWarehouseLocationId, createdAtUtc);
        foreach (CreateWarehouseTransferItemRequest item in request.Items)
            transfer.Items.Add(new WarehouseTransferItem(Guid.NewGuid(), transferId, item.ProductId, item.Quantity));
        dbContext.WarehouseTransfers.Add(transfer);
        await dbContext.SaveChangesAsync(ct);
        return transfer;
    }

    public Task<WarehouseTransfer?> GetAsync(Guid id, CancellationToken ct) => dbContext.WarehouseTransfers
        .AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<WarehouseTransfer> CompleteAsync(Guid id, DateTimeOffset completedAtUtc, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        WarehouseTransfer transfer = await dbContext.FindWarehouseTransferForUpdateAsync(id, ct)
            ?? throw new NotFoundException("Warehouse transfer was not found.");
        if (transfer.Status == WarehouseTransferStatus.Completed)
            throw new ConflictException("Warehouse transfer is already completed.");
        await EnsurePositionAsync(transfer.SourceWarehouseId, transfer.SourceWarehouseLocationId, "Source", ct);
        await EnsurePositionAsync(transfer.DestinationWarehouseId, transfer.DestinationWarehouseLocationId, "Destination", ct);

        var lockedBalances = new Dictionary<(Guid ProductId, Guid WarehouseId, Guid LocationId), InventoryBalance?>();
        var balanceKeys = transfer.Items
            .SelectMany(item => new[]
            {
                (ProductId: item.ProductId, WarehouseId: transfer.SourceWarehouseId, LocationId: transfer.SourceWarehouseLocationId),
                (ProductId: item.ProductId, WarehouseId: transfer.DestinationWarehouseId, LocationId: transfer.DestinationWarehouseLocationId)
            })
            .Distinct()
            .OrderBy(x => x.ProductId).ThenBy(x => x.WarehouseId).ThenBy(x => x.LocationId);
        foreach (var key in balanceKeys)
            lockedBalances[key] = await FindBalanceForUpdateAsync(key.ProductId, key.WarehouseId, key.LocationId, ct);

        var sourceBalances = new Dictionary<Guid, InventoryBalance>();
        foreach (WarehouseTransferItem item in transfer.Items.OrderBy(x => x.ProductId))
        {
            InventoryBalance? balance = lockedBalances[(item.ProductId, transfer.SourceWarehouseId,
                transfer.SourceWarehouseLocationId)];
            if (balance is null)
                throw new ConflictException($"No source inventory balance exists for product '{item.ProductId}'.");
            if (item.Quantity > balance.AvailableQuantity)
                throw new ConflictException($"Insufficient available stock for product '{item.ProductId}'.");
            sourceBalances[item.ProductId] = balance;
        }

        DateTimeOffset timestamp = completedAtUtc.ToUniversalTime();
        foreach (WarehouseTransferItem item in transfer.Items.OrderBy(x => x.ProductId))
        {
            InventoryBalance source = sourceBalances[item.ProductId];
            source.Issue(item.Quantity);
            InventoryBalance? destination = lockedBalances[(item.ProductId, transfer.DestinationWarehouseId,
                transfer.DestinationWarehouseLocationId)];
            if (destination is null)
            {
                destination = new InventoryBalance(item.ProductId, transfer.DestinationWarehouseId,
                    transfer.DestinationWarehouseLocationId, item.Quantity, 0m);
                dbContext.InventoryBalances.Add(destination);
            }
            else destination.Receive(item.Quantity);

            Guid outId = Guid.NewGuid();
            Guid inId = Guid.NewGuid();
            string referenceId = transfer.Id.ToString("D");
            dbContext.StockMovements.Add(new StockMovement(outId, item.ProductId, transfer.SourceWarehouseId,
                transfer.SourceWarehouseLocationId, StockMovementType.TransferOut, item.Quantity,
                "WarehouseTransfer", referenceId, timestamp));
            dbContext.StockMovements.Add(new StockMovement(inId, item.ProductId, transfer.DestinationWarehouseId,
                transfer.DestinationWarehouseLocationId, StockMovementType.TransferIn, item.Quantity,
                "WarehouseTransfer", referenceId, timestamp));
            item.AttachMovements(outId, inId);
        }
        transfer.Complete(timestamp);
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return transfer;
    }

    public async Task<PagedResult<WarehouseTransfer>> ListAsync(WarehouseTransferHistoryQuery query, CancellationToken ct)
    {
        int count = await dbContext.WarehouseTransfers.CountAsync(ct);
        int offset = (query.PageNumber - 1) * query.PageSize;
        List<WarehouseTransfer> transfers = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
            ? await dbContext.WarehouseTransfers.FromSqlInterpolated($"""
                SELECT * FROM WarehouseTransfers ORDER BY CreatedAtUtc DESC, Id DESC
                LIMIT {query.PageSize} OFFSET {offset}
                """).AsNoTracking().Include(x => x.Items).ToListAsync(ct)
            : await dbContext.WarehouseTransfers.AsNoTracking().Include(x => x.Items)
                .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
                .Skip(offset).Take(query.PageSize).ToListAsync(ct);
        return new(transfers, query.PageNumber, query.PageSize, count);
    }

    private Task<InventoryBalance?> FindBalanceAsync(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct) =>
        dbContext.InventoryBalances.AsNoTracking().SingleOrDefaultAsync(x => x.ProductId == productId &&
            x.WarehouseId == warehouseId && x.WarehouseLocationId == locationId, ct);

    private Task<InventoryBalance?> FindBalanceForUpdateAsync(Guid productId, Guid warehouseId, Guid locationId, CancellationToken ct) =>
        dbContext.FindInventoryBalanceForUpdateAsync(productId, warehouseId, locationId, ct);

    private async Task EnsurePositionAsync(Guid warehouseId, Guid locationId, string role, CancellationToken ct)
    {
        if (!await dbContext.Warehouses.AnyAsync(x => x.Id == warehouseId, ct))
            throw new NotFoundException($"{role} warehouse was not found.");
        if (!await dbContext.WarehouseLocations.AnyAsync(x => x.WarehouseId == warehouseId && x.Id == locationId, ct))
            throw new NotFoundException($"{role} warehouse location was not found.");
    }
}
