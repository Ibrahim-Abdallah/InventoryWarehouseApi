using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.InventoryReservations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;using InventoryWarehouseApi.Application.Authentication;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/inventory-reservations")]
[Authorize(Policy=AuthorizationPolicies.InventoryRead)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,"application/problem+json",Description="The request failed validation.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized,"application/problem+json",Description="Authentication is required or the access token is invalid.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden,"application/problem+json",Description="The authenticated user lacks the required permission.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError,"application/problem+json",Description="An unexpected server error occurred.")]
public sealed class InventoryReservationsController(IInventoryReservationService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    [EndpointSummary("Create inventory reservation")][EndpointDescription("Allocates AvailableQuantity at one exact position without changing physical stock or creating a movement.")]
    [ProducesResponseType<InventoryReservationResponse>(StatusCodes.Status201Created,Description="Creates and returns the Active reservation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The requested inventory position was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="Available quantity is insufficient or current state conflicts.")]
    public async Task<ActionResult<InventoryReservationResponse>> Create(CreateInventoryReservationRequest request, CancellationToken ct)
    { var reservation = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = reservation.Id }, reservation); }
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get inventory reservation")][EndpointDescription("Returns one reservation with its lifecycle timestamps and fulfillment movement link.")]
    [ProducesResponseType<InventoryReservationResponse>(StatusCodes.Status200OK,Description="Returns the requested reservation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The reservation was not found.")]
    public Task<InventoryReservationResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpGet]
    [EndpointSummary("List inventory reservations")][EndpointDescription("Returns newest-first paged inventory reservation history.")]
    [ProducesResponseType<PagedResult<InventoryReservationResponse>>(StatusCodes.Status200OK,Description="Returns the requested reservation page.")]
    public Task<PagedResult<InventoryReservationResponse>> List([FromQuery] InventoryReservationHistoryQuery query, CancellationToken ct) => service.ListAsync(query, ct);
    [HttpPost("{id:guid}/release")]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    [EndpointSummary("Release inventory reservation")][EndpointDescription("Releases the full Active reservation back to availability without creating a physical stock movement.")]
    [ProducesResponseType<InventoryReservationResponse>(StatusCodes.Status200OK,Description="Returns the Released reservation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The reservation was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The reservation is not Active.")]
    public Task<InventoryReservationResponse> Release(Guid id, CancellationToken ct) => service.ReleaseAsync(id, ct);
    [HttpPost("{id:guid}/fulfill")]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    [EndpointSummary("Fulfill inventory reservation")][EndpointDescription("Consumes the full Active reservation from OnHand and Reserved quantities and creates one linked StockOut movement.")]
    [ProducesResponseType<InventoryReservationResponse>(StatusCodes.Status200OK,Description="Returns the Fulfilled reservation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The reservation or inventory position was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The reservation is not Active or current state conflicts.")]
    public Task<InventoryReservationResponse> Fulfill(Guid id, CancellationToken ct) => service.FulfillAsync(id, ct);
}
