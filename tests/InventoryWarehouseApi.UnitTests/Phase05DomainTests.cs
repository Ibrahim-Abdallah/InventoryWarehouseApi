using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.UnitTests;

public sealed class Phase05DomainTests
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();
    private readonly Guid _movementId = Guid.NewGuid();

    [Theory]
    [InlineData(InventoryAdjustmentType.Increase)]
    [InlineData(InventoryAdjustmentType.Decrease)]
    public void InventoryAdjustment_CreatesImmutableTrimmedAuditRecord(InventoryAdjustmentType type)
    {
        DateTimeOffset local = new(2026, 8, 23, 12, 0, 0, TimeSpan.FromHours(2));
        InventoryAdjustment adjustment = Create(type, 5m, " Physical count ", " manager ", local);
        Assert.Equal(type, adjustment.AdjustmentType);
        Assert.Equal("Physical count", adjustment.Reason);
        Assert.Equal("manager", adjustment.AdjustedBy);
        Assert.Equal(_movementId, adjustment.StockMovementId);
        Assert.Equal(TimeSpan.Zero, adjustment.AdjustedAtUtc.Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InventoryAdjustment_RejectsNonPositiveQuantity(decimal quantity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(InventoryAdjustmentType.Increase, quantity, "reason", "user"));

    [Theory]
    [InlineData(null, "user")]
    [InlineData("   ", "user")]
    [InlineData("reason", null)]
    [InlineData("reason", "   ")]
    public void InventoryAdjustment_RejectsBlankAuditStrings(string? reason, string? adjustedBy) =>
        Assert.Throws<ArgumentException>(() => Create(InventoryAdjustmentType.Increase, 1m, reason!, adjustedBy!));

    [Fact]
    public void InventoryAdjustment_RejectsExcessiveAuditStrings()
    {
        Assert.Throws<ArgumentException>(() => Create(InventoryAdjustmentType.Increase, 1m, new string('r', 501), "user"));
        Assert.Throws<ArgumentException>(() => Create(InventoryAdjustmentType.Increase, 1m, "reason", new string('u', 129)));
    }

    [Fact]
    public void InventoryAdjustment_RejectsEmptyDependenciesAndUnsupportedType()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => new InventoryAdjustment(Guid.Empty, _productId, _warehouseId, _locationId, InventoryAdjustmentType.Increase, 1m, "r", "u", _movementId, now));
        Assert.Throws<ArgumentException>(() => new InventoryAdjustment(Guid.NewGuid(), Guid.Empty, _warehouseId, _locationId, InventoryAdjustmentType.Increase, 1m, "r", "u", _movementId, now));
        Assert.Throws<ArgumentException>(() => new InventoryAdjustment(Guid.NewGuid(), _productId, Guid.Empty, _locationId, InventoryAdjustmentType.Increase, 1m, "r", "u", _movementId, now));
        Assert.Throws<ArgumentException>(() => new InventoryAdjustment(Guid.NewGuid(), _productId, _warehouseId, Guid.Empty, InventoryAdjustmentType.Increase, 1m, "r", "u", _movementId, now));
        Assert.Throws<ArgumentException>(() => new InventoryAdjustment(Guid.NewGuid(), _productId, _warehouseId, _locationId, InventoryAdjustmentType.Increase, 1m, "r", "u", Guid.Empty, now));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create((InventoryAdjustmentType)99, 1m, "r", "u"));
    }

    [Theory]
    [InlineData(StockMovementType.AdjustmentIncrease)]
    [InlineData(StockMovementType.AdjustmentDecrease)]
    public void StockMovement_AcceptsAdjustmentTypes(StockMovementType type) =>
        Assert.Equal(type, new StockMovement(_productId, _warehouseId, _locationId, type, 1m,
            "InventoryAdjustment", Guid.NewGuid().ToString("D"), DateTimeOffset.UtcNow).MovementType);

    [Fact]
    public void AdjustmentBalanceSemantics_PreserveReservedAndUseAvailable()
    {
        InventoryBalance balance = new(_productId, _warehouseId, _locationId, 10m, 4m);
        balance.Receive(2m);
        Assert.Equal((12m, 4m), (balance.OnHandQuantity, balance.ReservedQuantity));
        balance.Issue(8m);
        Assert.Equal((4m, 4m, 0m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
        Assert.Throws<InvalidOperationException>(() => balance.Issue(1m));
    }

    [Fact]
    public void Phase05Validators_RejectInvalidRequestsAndPaging()
    {
        InventoryAdjustmentRequestValidator validator = new();
        Assert.False(validator.Validate(new InventoryAdjustmentRequest(1.0001m, "reason", "user")).IsValid);
        Assert.False(validator.Validate(new InventoryAdjustmentRequest(1m, " ", "user")).IsValid);
        Assert.False(validator.Validate(new InventoryAdjustmentRequest(1m, "reason", " ")).IsValid);
        Assert.False(new InventoryAdjustmentHistoryQueryValidator().Validate(new InventoryAdjustmentHistoryQuery(0, 101)).IsValid);
    }

    private InventoryAdjustment Create(InventoryAdjustmentType type, decimal quantity, string reason,
        string adjustedBy, DateTimeOffset? timestamp = null) => new(Guid.NewGuid(), _productId, _warehouseId,
        _locationId, type, quantity, reason, adjustedBy, _movementId, timestamp ?? DateTimeOffset.UtcNow);
}
