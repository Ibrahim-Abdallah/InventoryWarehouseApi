using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.WarehouseTransfers;

public interface IWarehouseTransferRepository
{
    Task<WarehouseTransfer> CreateAsync(CreateWarehouseTransferRequest request, DateTimeOffset createdAtUtc, CancellationToken ct);
    Task<WarehouseTransfer?> GetAsync(Guid id, CancellationToken ct);
    Task<WarehouseTransfer> CompleteAsync(Guid id, DateTimeOffset completedAtUtc, CancellationToken ct);
    Task<PagedResult<WarehouseTransfer>> ListAsync(WarehouseTransferHistoryQuery query, CancellationToken ct);
}

public interface IWarehouseTransferService
{
    Task<WarehouseTransferResponse> CreateAsync(CreateWarehouseTransferRequest request, CancellationToken ct);
    Task<WarehouseTransferResponse> GetAsync(Guid id, CancellationToken ct);
    Task<WarehouseTransferResponse> CompleteAsync(Guid id, CancellationToken ct);
    Task<PagedResult<WarehouseTransferSummaryResponse>> ListAsync(WarehouseTransferHistoryQuery query, CancellationToken ct);
}
