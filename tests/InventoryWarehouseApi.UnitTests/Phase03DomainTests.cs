using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Application.WarehouseLocations;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.UnitTests;

public sealed class Phase03DomainTests
{
    [Fact]
    public void WarehouseLocation_Normalizes_And_UpdatePreservesIdentity()
    {
        DateTimeOffset created = new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        Guid warehouseId = Guid.NewGuid();
        WarehouseLocation location = new(warehouseId, " a-01 ", " Shelf A ", " ", created);
        Guid id = location.Id;

        Assert.Equal("A-01", location.Code);
        Assert.Equal("Shelf A", location.Name);
        Assert.Null(location.Description);
        location.Update(" b-02 ", " Shelf B ", " Near door ", false, created.AddHours(1));

        Assert.Equal(id, location.Id);
        Assert.Equal(warehouseId, location.WarehouseId);
        Assert.Equal(created, location.CreatedAtUtc);
        Assert.Equal("B-02", location.Code);
        Assert.False(location.IsActive);
    }

    [Fact]
    public void InventoryBalance_DerivesAvailableQuantity() =>
        Assert.Equal(7m, new InventoryBalance(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m, 3m).AvailableQuantity);

    [Fact]
    public void InventoryBalance_AllowsZeroQuantities() =>
        Assert.Equal(0m, new InventoryBalance(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, 0m).AvailableQuantity);

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 2)]
    public void InventoryBalance_RejectsInvalidQuantities(decimal onHand, decimal reserved) =>
        Assert.ThrowsAny<ArgumentException>(() => new InventoryBalance(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), onHand, reserved));

    [Fact]
    public void Phase03Validators_RejectInvalidPagingAndSorting()
    {
        WarehouseLocationQuery locationQuery = new(0, 101, SortBy: "bad", SortDirection: "sideways");
        LocationInventoryQuery inventoryQuery = new(0, 101, SortBy: "quantity", SortDirection: "sideways");
        CreateWarehouseLocationRequest create = new("", "", new string('x', 1001));
        Assert.False(new WarehouseLocationQueryValidator().Validate(locationQuery).IsValid);
        Assert.False(new LocationInventoryQueryValidator().Validate(inventoryQuery).IsValid);
        Assert.False(new CreateWarehouseLocationValidator().Validate(create).IsValid);
    }
}
