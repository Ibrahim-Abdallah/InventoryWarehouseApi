using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.WarehouseTransfers;

internal sealed class WarehouseTransferService(IWarehouseTransferRepository repository,
    IValidator<CreateWarehouseTransferRequest> createValidator,
    IValidator<WarehouseTransferHistoryQuery> queryValidator) : IWarehouseTransferService
{
    public async Task<WarehouseTransferResponse> CreateAsync(CreateWarehouseTransferRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        return Map(await repository.CreateAsync(request, DateTimeOffset.UtcNow, ct));
    }

    public async Task<WarehouseTransferResponse> GetAsync(Guid id, CancellationToken ct) =>
        Map(await repository.GetAsync(id, ct) ?? throw new NotFoundException("Warehouse transfer was not found."));

    public async Task<WarehouseTransferResponse> CompleteAsync(Guid id, CancellationToken ct) =>
        Map(await repository.CompleteAsync(id, DateTimeOffset.UtcNow, ct));

    public async Task<PagedResult<WarehouseTransferSummaryResponse>> ListAsync(WarehouseTransferHistoryQuery query, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);
        PagedResult<WarehouseTransfer> page = await repository.ListAsync(query, ct);
        return new(page.Items.Select(x => new WarehouseTransferSummaryResponse(x.Id, x.SourceWarehouseId,
            x.SourceWarehouseLocationId, x.DestinationWarehouseId, x.DestinationWarehouseLocationId,
            x.Status, x.Items.Count, x.CreatedAtUtc, x.CompletedAtUtc)).ToList(), page.PageNumber, page.PageSize, page.TotalCount);
    }

    private static WarehouseTransferResponse Map(WarehouseTransfer x) => new(x.Id, x.SourceWarehouseId,
        x.SourceWarehouseLocationId, x.DestinationWarehouseId, x.DestinationWarehouseLocationId,
        x.Status, x.CreatedAtUtc, x.CompletedAtUtc, x.Items.OrderBy(i => i.ProductId).ThenBy(i => i.Id)
            .Select(i => new WarehouseTransferItemResponse(i.Id, i.ProductId, i.Quantity,
                i.TransferOutMovementId, i.TransferInMovementId)).ToList());
}
