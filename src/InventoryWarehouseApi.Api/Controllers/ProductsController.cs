using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    public Task<PagedResult<ProductResponse>> List([FromQuery] ProductQuery query, CancellationToken ct) => service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<ProductResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        ProductResponse product = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    public Task<ProductResponse> Update(Guid id, UpdateProductRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
