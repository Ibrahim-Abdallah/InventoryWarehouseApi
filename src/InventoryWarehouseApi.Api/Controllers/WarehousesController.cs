using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Warehouses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;using InventoryWarehouseApi.Application.Authentication;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize(Policy=AuthorizationPolicies.CatalogRead)]
public sealed class WarehousesController(IWarehouseService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<WarehouseResponse>>(StatusCodes.Status200OK)]
    public Task<PagedResult<WarehouseResponse>> List([FromQuery] WarehouseQuery query, CancellationToken ct) => service.ListAsync(query, ct);
    [HttpGet("{id:guid}")]
    public Task<WarehouseResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpPost]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [ProducesResponseType<WarehouseResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<WarehouseResponse>> Create(CreateWarehouseRequest request, CancellationToken ct)
    {
        WarehouseResponse warehouse = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = warehouse.Id }, warehouse);
    }
    [HttpPut("{id:guid}")]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    public Task<WarehouseResponse> Update(Guid id, UpdateWarehouseRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);
    [HttpDelete("{id:guid}")]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
