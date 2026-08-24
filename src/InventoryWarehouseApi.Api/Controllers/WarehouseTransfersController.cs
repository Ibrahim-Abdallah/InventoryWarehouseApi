using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.WarehouseTransfers;
using Microsoft.AspNetCore.Mvc;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/warehouse-transfers")]
public sealed class WarehouseTransfersController(IWarehouseTransferService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WarehouseTransferResponse>> Create(CreateWarehouseTransferRequest request, CancellationToken ct)
    {
        WarehouseTransferResponse transfer = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = transfer.Id }, transfer);
    }

    [HttpGet("{id:guid}")]
    public Task<WarehouseTransferResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpGet]
    public Task<PagedResult<WarehouseTransferSummaryResponse>> List([FromQuery] WarehouseTransferHistoryQuery query,
        CancellationToken ct) => service.ListAsync(query, ct);

    [HttpPost("{id:guid}/complete")]
    public Task<WarehouseTransferResponse> Complete(Guid id, CancellationToken ct) => service.CompleteAsync(id, ct);
}
