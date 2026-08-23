using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.UnitTests;

public sealed class Phase04DomainTests
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();

    [Fact]
    public void Receive_IncreasesOnHand_AndPreservesReserved()
    {
        InventoryBalance balance = Balance(10m, 4m);
        balance.Receive(2.5m);
        Assert.Equal((12.5m, 4m, 8.5m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Receive_RejectsNonPositiveQuantity(decimal quantity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Balance(10m, 4m).Receive(quantity));

    [Fact]
    public void Issue_DecreasesOnHand_AndPreservesReserved()
    {
        InventoryBalance balance = Balance(10m, 4m);
        balance.Issue(2m);
        Assert.Equal((8m, 4m, 4m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
    }

    [Fact]
    public void Issue_EqualToAvailable_SucceedsAndCanReachOnHandEqualReserved()
    {
        InventoryBalance balance = Balance(10m, 4m);
        balance.Issue(6m);
        Assert.Equal((4m, 4m, 0m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
    }

    [Fact]
    public void Issue_GreaterThanAvailable_FailsWithoutMutation()
    {
        InventoryBalance balance = Balance(10m, 4m);
        Assert.Throws<InvalidOperationException>(() => balance.Issue(7m));
        Assert.Equal((10m, 4m), (balance.OnHandQuantity, balance.ReservedQuantity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Issue_RejectsNonPositiveQuantity(decimal quantity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Balance(10m, 4m).Issue(quantity));

    [Theory]
    [InlineData(StockMovementType.StockIn)]
    [InlineData(StockMovementType.StockOut)]
    public void StockMovement_CreatesSupportedPositiveMovement(StockMovementType type)
    {
        StockMovement movement = Movement(type, 3m, " PurchaseReceipt ", " PR-1001 ");
        Assert.NotEqual(Guid.Empty, movement.Id);
        Assert.Equal(type, movement.MovementType);
        Assert.Equal(3m, movement.Quantity);
        Assert.Equal("PurchaseReceipt", movement.ReferenceType);
        Assert.Equal("PR-1001", movement.ReferenceId);
    }

    [Fact]
    public void StockMovement_AllowsNullReferencePair()
    {
        StockMovement movement = Movement(StockMovementType.StockIn, 1m, null, null);
        Assert.Null(movement.ReferenceType);
        Assert.Null(movement.ReferenceId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void StockMovement_RejectsNonPositiveQuantity(decimal quantity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Movement(StockMovementType.StockIn, quantity, null, null));

    [Fact]
    public void StockMovement_RejectsEmptyDependencies()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => new StockMovement(Guid.Empty, _warehouseId, _locationId, StockMovementType.StockIn, 1m, null, null, now));
        Assert.Throws<ArgumentException>(() => new StockMovement(_productId, Guid.Empty, _locationId, StockMovementType.StockIn, 1m, null, null, now));
        Assert.Throws<ArgumentException>(() => new StockMovement(_productId, _warehouseId, Guid.Empty, StockMovementType.StockIn, 1m, null, null, now));
    }

    [Fact]
    public void StockMovement_RejectsUnsupportedType() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Movement((StockMovementType)99, 1m, null, null));

    [Theory]
    [InlineData("Order", null)]
    [InlineData(null, "O-1")]
    [InlineData("   ", "O-1")]
    public void StockMovement_RejectsOneSidedReference(string? type, string? id) =>
        Assert.Throws<ArgumentException>(() => Movement(StockMovementType.StockOut, 1m, type, id));

    [Fact]
    public void StockMovement_RejectsExcessiveReferences()
    {
        Assert.Throws<ArgumentException>(() => Movement(StockMovementType.StockIn, 1m, new string('x', 65), "id"));
        Assert.Throws<ArgumentException>(() => Movement(StockMovementType.StockIn, 1m, "type", new string('x', 129)));
    }

    [Fact]
    public void Phase04Validators_RejectInvalidQuantityReferencesAndPaging()
    {
        Assert.False(new StockMovementRequestValidator().Validate(new StockMovementRequest(1.0001m, "type", null)).IsValid);
        Assert.False(new StockMovementHistoryQueryValidator().Validate(new StockMovementHistoryQuery(0, 101)).IsValid);
    }

    private InventoryBalance Balance(decimal onHand, decimal reserved) => new(_productId, _warehouseId, _locationId, onHand, reserved);
    private StockMovement Movement(StockMovementType type, decimal quantity, string? referenceType, string? referenceId) =>
        new(_productId, _warehouseId, _locationId, type, quantity, referenceType, referenceId, DateTimeOffset.UtcNow);
}
