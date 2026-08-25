using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.InventoryReservations;

public interface IInventoryReservationRepository
{
    Task<InventoryReservation> CreateAsync(CreateInventoryReservationRequest request, DateTimeOffset timestamp, CancellationToken ct);
    Task<InventoryReservation?> GetAsync(Guid id, CancellationToken ct);
    Task<InventoryReservation> ReleaseAsync(Guid id, DateTimeOffset timestamp, CancellationToken ct);
    Task<InventoryReservation> FulfillAsync(Guid id, DateTimeOffset timestamp, CancellationToken ct);
    Task<PagedResult<InventoryReservation>> ListAsync(InventoryReservationHistoryQuery query, CancellationToken ct);
}

public interface IInventoryReservationService
{
    Task<InventoryReservationResponse> CreateAsync(CreateInventoryReservationRequest request, CancellationToken ct);
    Task<InventoryReservationResponse> GetAsync(Guid id, CancellationToken ct);
    Task<InventoryReservationResponse> ReleaseAsync(Guid id, CancellationToken ct);
    Task<InventoryReservationResponse> FulfillAsync(Guid id, CancellationToken ct);
    Task<PagedResult<InventoryReservationResponse>> ListAsync(InventoryReservationHistoryQuery query, CancellationToken ct);
}
