using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.UnitTests;

public sealed class Phase07DomainTests
{
    [Fact]
    public void ReserveReleaseAndFulfill_ApplyReservationArithmetic()
    {
        InventoryBalance balance = Balance(10m, 0m);
        balance.Reserve(4m);
        Assert.Equal((10m, 4m, 6m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
        balance.ReleaseReservation(4m);
        Assert.Equal((10m, 0m, 10m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
        balance.Reserve(4m);
        balance.FulfillReservation(4m);
        Assert.Equal((6m, 0m, 6m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)]
    public void ReservationBalanceOperations_RejectNonPositive(decimal quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Balance(10m, 2m).Reserve(quantity));
        Assert.Throws<ArgumentOutOfRangeException>(() => Balance(10m, 2m).ReleaseReservation(quantity));
        Assert.Throws<ArgumentOutOfRangeException>(() => Balance(10m, 2m).FulfillReservation(quantity));
    }

    [Fact]
    public void ReservationBalanceOperations_RejectInsufficientQuantities()
    {
        Assert.Throws<InvalidOperationException>(() => Balance(10m, 4m).Reserve(7m));
        Assert.Throws<InvalidOperationException>(() => Balance(10m, 4m).ReleaseReservation(5m));
        Assert.Throws<InvalidOperationException>(() => Balance(10m, 4m).FulfillReservation(5m));
    }

    [Fact]
    public void Reservation_CreationNormalizesReferencesAndUtc()
    {
        InventoryReservation reservation = Create(" Order ", " 42 ");
        Assert.Equal(InventoryReservationStatus.Active, reservation.Status);
        Assert.Equal(("Order", "42"), (reservation.ReferenceType, reservation.ReferenceId));
        Assert.Equal(TimeSpan.Zero, reservation.CreatedAtUtc.Offset);
        Assert.Null(reservation.ReleasedAtUtc); Assert.Null(reservation.FulfilledAtUtc); Assert.Null(reservation.FulfillmentMovementId);
    }

    [Fact]
    public void Release_TransitionsOnceAndPreventsFulfillment()
    {
        InventoryReservation reservation = Create();
        reservation.Release(reservation.CreatedAtUtc.AddMinutes(1));
        Assert.Equal(InventoryReservationStatus.Released, reservation.Status);
        Assert.NotNull(reservation.ReleasedAtUtc);
        Assert.Throws<InvalidOperationException>(() => reservation.Release(DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => reservation.Fulfill(DateTimeOffset.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void Fulfill_TransitionsOnceAndStoresMovement()
    {
        InventoryReservation reservation = Create(); Guid movementId = Guid.NewGuid();
        reservation.Fulfill(reservation.CreatedAtUtc.AddMinutes(1), movementId);
        Assert.Equal(InventoryReservationStatus.Fulfilled, reservation.Status);
        Assert.Equal(movementId, reservation.FulfillmentMovementId);
        Assert.NotNull(reservation.FulfilledAtUtc); Assert.Null(reservation.ReleasedAtUtc);
        Assert.Throws<InvalidOperationException>(() => reservation.Fulfill(DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => reservation.Release(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Reservation_EnforcesReferencesIdsQuantityAndTimestamps()
    {
        Assert.Throws<ArgumentException>(() => Create("Order", null));
        Assert.Throws<ArgumentException>(() => new InventoryReservation(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, null, null, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InventoryReservation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, null, null, DateTimeOffset.UtcNow));
        InventoryReservation release = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => release.Release(release.CreatedAtUtc.AddTicks(-1)));
        InventoryReservation fulfill = Create();
        Assert.Throws<ArgumentException>(() => fulfill.Fulfill(DateTimeOffset.UtcNow, Guid.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => fulfill.Fulfill(fulfill.CreatedAtUtc.AddTicks(-1), Guid.NewGuid()));
    }

    private static InventoryBalance Balance(decimal onHand, decimal reserved) => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), onHand, reserved);
    private static InventoryReservation Create(string? type = null, string? id = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 4m, type, id,
            new DateTimeOffset(2026, 8, 24, 20, 0, 0, TimeSpan.FromHours(2)));
}
