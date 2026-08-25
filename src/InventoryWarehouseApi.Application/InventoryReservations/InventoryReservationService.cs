using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.InventoryReservations;

internal sealed class InventoryReservationService(IInventoryReservationRepository repository,
    IValidator<CreateInventoryReservationRequest> createValidator,
    IValidator<InventoryReservationHistoryQuery> queryValidator) : IInventoryReservationService
{
    public async Task<InventoryReservationResponse> CreateAsync(CreateInventoryReservationRequest request, CancellationToken ct)
    { await createValidator.ValidateAndThrowAsync(request, ct); return Map(await repository.CreateAsync(request, DateTimeOffset.UtcNow, ct)); }
    public async Task<InventoryReservationResponse> GetAsync(Guid id, CancellationToken ct) =>
        Map(await repository.GetAsync(id, ct) ?? throw new NotFoundException("Inventory reservation was not found."));
    public async Task<InventoryReservationResponse> ReleaseAsync(Guid id, CancellationToken ct) =>
        Map(await repository.ReleaseAsync(id, DateTimeOffset.UtcNow, ct));
    public async Task<InventoryReservationResponse> FulfillAsync(Guid id, CancellationToken ct) =>
        Map(await repository.FulfillAsync(id, DateTimeOffset.UtcNow, ct));
    public async Task<PagedResult<InventoryReservationResponse>> ListAsync(InventoryReservationHistoryQuery query, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);
        PagedResult<InventoryReservation> page = await repository.ListAsync(query, ct);
        return new(page.Items.Select(Map).ToList(), page.PageNumber, page.PageSize, page.TotalCount);
    }
    private static InventoryReservationResponse Map(InventoryReservation x) => new(x.Id, x.ProductId, x.WarehouseId,
        x.WarehouseLocationId, x.Quantity, x.Status, x.ReferenceType, x.ReferenceId, x.CreatedAtUtc,
        x.ReleasedAtUtc, x.FulfilledAtUtc, x.FulfillmentMovementId);
}
