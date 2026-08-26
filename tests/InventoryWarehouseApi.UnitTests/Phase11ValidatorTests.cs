using InventoryWarehouseApi.Application.Inventory;
using InventoryWarehouseApi.Application.InventoryReservations;
using InventoryWarehouseApi.Application.Reporting;
using InventoryWarehouseApi.Application.WarehouseTransfers;

namespace InventoryWarehouseApi.UnitTests;

public sealed class Phase11ValidatorTests
{
    [Fact]
    public void ReportSortDirection_NullIsRejectedWithoutThrowing()
    {
        InventorySummaryQuery query = new(SortDirection: null!);
        var exception = Record.Exception(() => new InventorySummaryQueryValidator().Validate(query));

        Assert.Null(exception);
        Assert.False(new InventorySummaryQueryValidator().Validate(query).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.0001)]
    public void QuantityValidators_RejectNonPositiveOrExcessScale(decimal quantity)
    {
        Assert.False(new StockMovementRequestValidator().Validate(new StockMovementRequest(quantity)).IsValid);
        Assert.False(new InventoryAdjustmentRequestValidator().Validate(new InventoryAdjustmentRequest(quantity, "cycle count")).IsValid);
        Assert.False(new CreateInventoryReservationValidator().Validate(
            new CreateInventoryReservationRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantity, null, null)).IsValid);
    }

    [Theory]
    [InlineData(0, 20, false)]
    [InlineData(1, 101, false)]
    [InlineData(1, 100, true)]
    public void HistoryValidators_EnforcePagingBoundaries(int pageNumber, int pageSize, bool valid)
    {
        Assert.Equal(valid, new StockMovementHistoryQueryValidator().Validate(new StockMovementHistoryQuery(pageNumber, pageSize)).IsValid);
        Assert.Equal(valid, new InventoryAdjustmentHistoryQueryValidator().Validate(new InventoryAdjustmentHistoryQuery(pageNumber, pageSize)).IsValid);
        Assert.Equal(valid, new WarehouseTransferHistoryQueryValidator().Validate(new WarehouseTransferHistoryQuery(pageNumber, pageSize)).IsValid);
        Assert.Equal(valid, new InventoryReservationHistoryQueryValidator().Validate(new InventoryReservationHistoryQuery(pageNumber, pageSize)).IsValid);
    }

    [Fact]
    public void StockMovementReferences_MustBePairedAndBounded()
    {
        StockMovementRequestValidator validator = new();
        Assert.False(validator.Validate(new StockMovementRequest(1, "Order", null)).IsValid);
        Assert.False(validator.Validate(new StockMovementRequest(1, null, "123")).IsValid);
        Assert.False(validator.Validate(new StockMovementRequest(1, new string('t', 65), new string('i', 129))).IsValid);
        Assert.True(validator.Validate(new StockMovementRequest(1, "Order", "123")).IsValid);
    }

    [Fact]
    public void TransferValidator_RejectsSamePositionAndDuplicateProducts()
    {
        Guid warehouse = Guid.NewGuid(), location = Guid.NewGuid(), product = Guid.NewGuid();
        CreateWarehouseTransferValidator validator = new();
        CreateWarehouseTransferRequest request = new(warehouse, location, warehouse, location,
            [new(product, 1), new(product, 2)]);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("differ"));
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("Duplicate"));
    }

    [Fact]
    public void ReservationReferences_MustBePairedAndBounded()
    {
        CreateInventoryReservationValidator validator = new();
        Guid id = Guid.NewGuid();
        Assert.False(validator.Validate(new CreateInventoryReservationRequest(id, id, id, 1, "Order", null)).IsValid);
        Assert.False(validator.Validate(new CreateInventoryReservationRequest(id, id, id, 1, new string('t', 65), new string('i', 129))).IsValid);
        Assert.True(validator.Validate(new CreateInventoryReservationRequest(id, id, id, 1, "Order", "123")).IsValid);
    }
}
