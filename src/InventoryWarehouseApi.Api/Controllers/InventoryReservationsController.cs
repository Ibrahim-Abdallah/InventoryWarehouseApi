using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.InventoryReservations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;using InventoryWarehouseApi.Application.Authentication;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/inventory-reservations")]
[Authorize(Policy=AuthorizationPolicies.InventoryRead)]
public sealed class InventoryReservationsController(IInventoryReservationService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    public async Task<ActionResult<InventoryReservationResponse>> Create(CreateInventoryReservationRequest request, CancellationToken ct)
    { var reservation = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = reservation.Id }, reservation); }
    [HttpGet("{id:guid}")]
    public Task<InventoryReservationResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpGet]
    public Task<PagedResult<InventoryReservationResponse>> List([FromQuery] InventoryReservationHistoryQuery query, CancellationToken ct) => service.ListAsync(query, ct);
    [HttpPost("{id:guid}/release")]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    public Task<InventoryReservationResponse> Release(Guid id, CancellationToken ct) => service.ReleaseAsync(id, ct);
    [HttpPost("{id:guid}/fulfill")]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    public Task<InventoryReservationResponse> Fulfill(Guid id, CancellationToken ct) => service.FulfillAsync(id, ct);
}
