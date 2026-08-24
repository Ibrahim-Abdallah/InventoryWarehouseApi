using InventoryWarehouseApi.Application.WarehouseTransfers;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.UnitTests;

public sealed class Phase06DomainTests
{
    [Fact]
    public void Transfer_StartsPending_NormalizesUtc_AndCompletesOnce()
    {
        DateTimeOffset local = new(2026, 8, 24, 12, 0, 0, TimeSpan.FromHours(2));
        WarehouseTransfer transfer = Create(local);
        Assert.Equal(WarehouseTransferStatus.Pending, transfer.Status);
        Assert.Null(transfer.CompletedAtUtc);
        Assert.Equal(TimeSpan.Zero, transfer.CreatedAtUtc.Offset);
        transfer.Complete(local.AddHours(1));
        Assert.Equal(WarehouseTransferStatus.Completed, transfer.Status);
        Assert.Equal(TimeSpan.Zero, transfer.CompletedAtUtc!.Value.Offset);
        Assert.Throws<InvalidOperationException>(() => transfer.Complete(local.AddHours(2)));
    }

    [Fact]
    public void Transfer_AllowsSameWarehouseDifferentLocations_ButRejectsSamePosition()
    {
        Guid warehouse = Guid.NewGuid();
        _ = new WarehouseTransfer(Guid.NewGuid(), warehouse, Guid.NewGuid(), warehouse, Guid.NewGuid(), DateTimeOffset.UtcNow);
        Guid location = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new WarehouseTransfer(Guid.NewGuid(), warehouse, location,
            warehouse, location, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Transfer_RejectsEmptyRequiredIds()
    {
        Guid id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new WarehouseTransfer(Guid.Empty, id, id, id, Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new WarehouseTransfer(id, Guid.Empty, id, id, Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new WarehouseTransfer(id, id, Guid.Empty, id, Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new WarehouseTransfer(id, id, id, Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new WarehouseTransfer(id, id, id, id, Guid.Empty, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Item_EnforcesQuantityDependenciesAndImmutableMovementPair()
    {
        Guid transferId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        WarehouseTransferItem item = new(Guid.NewGuid(), transferId, productId, 2.5m);
        Assert.Null(item.TransferOutMovementId);
        Assert.Null(item.TransferInMovementId);
        Assert.Throws<ArgumentOutOfRangeException>(() => new WarehouseTransferItem(Guid.NewGuid(), transferId, productId, 0));
        Assert.Throws<ArgumentException>(() => new WarehouseTransferItem(Guid.Empty, transferId, productId, 1));
        Assert.Throws<ArgumentException>(() => item.AttachMovements(Guid.NewGuid(), null));
        Guid same = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => item.AttachMovements(same, same));
        Guid outgoing = Guid.NewGuid();
        Guid incoming = Guid.NewGuid();
        item.AttachMovements(outgoing, incoming);
        Assert.Equal((outgoing, incoming), (item.TransferOutMovementId, item.TransferInMovementId));
        Assert.Throws<InvalidOperationException>(() => item.AttachMovements(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Theory]
    [InlineData(StockMovementType.TransferOut)]
    [InlineData(StockMovementType.TransferIn)]
    public void StockMovement_AcceptsTransferTypes(StockMovementType type) =>
        Assert.Equal(type, new StockMovement(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), type, 1m,
            "WarehouseTransfer", Guid.NewGuid().ToString("D"), DateTimeOffset.UtcNow).MovementType);

    [Fact]
    public void Validators_RejectDuplicateInvalidItemsAndPaging()
    {
        Guid product = Guid.NewGuid();
        CreateWarehouseTransferRequest request = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            [new(product, 1m), new(product, 1m)]);
        Assert.False(new CreateWarehouseTransferValidator().Validate(request).IsValid);
        Assert.False(new WarehouseTransferHistoryQueryValidator().Validate(new WarehouseTransferHistoryQuery(0, 101)).IsValid);
    }

    private static WarehouseTransfer Create(DateTimeOffset created) => new(Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), created);
}
