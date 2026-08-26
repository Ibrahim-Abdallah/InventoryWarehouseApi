using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.WarehouseTransfers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;using InventoryWarehouseApi.Application.Authentication;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/warehouse-transfers")]
[Authorize(Policy=AuthorizationPolicies.InventoryRead)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,"application/problem+json",Description="The request failed validation.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized,"application/problem+json",Description="Authentication is required or the access token is invalid.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden,"application/problem+json",Description="The authenticated user lacks the required permission.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError,"application/problem+json",Description="An unexpected server error occurred.")]
public sealed class WarehouseTransfersController(IWarehouseTransferService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    [EndpointSummary("Create warehouse transfer")][EndpointDescription("Creates a Pending multi-item transfer after validating its endpoints and current availability; creation does not reserve or move stock.")]
    [ProducesResponseType<WarehouseTransferResponse>(StatusCodes.Status201Created,Description="Creates and returns the Pending transfer.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="A product, warehouse, or location was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="Current inventory state cannot satisfy the transfer.")]
    public async Task<ActionResult<WarehouseTransferResponse>> Create(CreateWarehouseTransferRequest request, CancellationToken ct)
    {
        WarehouseTransferResponse transfer = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = transfer.Id }, transfer);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get warehouse transfer")][EndpointDescription("Returns a warehouse transfer with its item and linked movement details.")]
    [ProducesResponseType<WarehouseTransferResponse>(StatusCodes.Status200OK,Description="Returns the requested transfer.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The transfer was not found.")]
    public Task<WarehouseTransferResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpGet]
    [EndpointSummary("List warehouse transfers")][EndpointDescription("Returns newest-first paged warehouse transfer history.")]
    [ProducesResponseType<PagedResult<WarehouseTransferSummaryResponse>>(StatusCodes.Status200OK,Description="Returns the requested transfer page.")]
    public Task<PagedResult<WarehouseTransferSummaryResponse>> List([FromQuery] WarehouseTransferHistoryQuery query,
        CancellationToken ct) => service.ListAsync(query, ct);

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy=AuthorizationPolicies.InventoryOperate)]
    [EndpointSummary("Complete warehouse transfer")][EndpointDescription("Atomically revalidates source availability, moves all items, creates paired TransferOut and TransferIn movements, and marks the Pending transfer Completed.")]
    [ProducesResponseType<WarehouseTransferResponse>(StatusCodes.Status200OK,Description="Returns the completed transfer.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The transfer or a referenced inventory resource was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The transfer is not Pending or current availability is insufficient.")]
    public Task<WarehouseTransferResponse> Complete(Guid id, CancellationToken ct) => service.CompleteAsync(id, ct);
}
