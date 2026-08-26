using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;using InventoryWarehouseApi.Application.Authentication;

namespace InventoryWarehouseApi.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy=AuthorizationPolicies.CatalogRead)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,"application/problem+json",Description="The request failed validation.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized,"application/problem+json",Description="Authentication is required or the access token is invalid.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden,"application/problem+json",Description="The authenticated user lacks the required permission.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError,"application/problem+json",Description="An unexpected server error occurred.")]
public sealed class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    [EndpointSummary("List products")][EndpointDescription("Returns a filtered, sorted, and paged product catalog.")]
    public Task<PagedResult<ProductResponse>> List([FromQuery] ProductQuery query, CancellationToken ct) => service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get product")][EndpointDescription("Returns one product by identifier.")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK,Description="Returns the requested product.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The product was not found.")]
    public Task<ProductResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [EndpointSummary("Create product")][EndpointDescription("Creates a product with a normalized, case-insensitive unique SKU.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The SKU conflicts with an existing product.")]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        ProductResponse product = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [EndpointSummary("Update product")][EndpointDescription("Updates product catalog details and active status while preserving SKU uniqueness.")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK,Description="Returns the updated product.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The product was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="The SKU conflicts with an existing product.")]
    public Task<ProductResponse> Update(Guid id, UpdateProductRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    [Authorize(Policy=AuthorizationPolicies.CatalogManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Delete product")][EndpointDescription("Deletes a product when no dependent inventory records prevent removal.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound,"application/problem+json",Description="The product was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict,"application/problem+json",Description="Dependent records prevent deletion.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
