using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.InventoryReservations;
using Microsoft.AspNetCore.Mvc;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/inventory-reservations")]
public sealed class InventoryReservationsController(IInventoryReservationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<InventoryReservationResponse>> Create(CreateInventoryReservationRequest request, CancellationToken ct)
    { var reservation = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = reservation.Id }, reservation); }
    [HttpGet("{id:guid}")]
    public Task<InventoryReservationResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpGet]
    public Task<PagedResult<InventoryReservationResponse>> List([FromQuery] InventoryReservationHistoryQuery query, CancellationToken ct) => service.ListAsync(query, ct);
    [HttpPost("{id:guid}/release")]
    public Task<InventoryReservationResponse> Release(Guid id, CancellationToken ct) => service.ReleaseAsync(id, ct);
    [HttpPost("{id:guid}/fulfill")]
    public Task<InventoryReservationResponse> Fulfill(Guid id, CancellationToken ct) => service.FulfillAsync(id, ct);
}
