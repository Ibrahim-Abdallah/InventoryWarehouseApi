using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.Products;

internal sealed class ProductService(IProductRepository repository, IValidator<CreateProductRequest> createValidator,
    IValidator<UpdateProductRequest> updateValidator, IValidator<ProductQuery> queryValidator) : IProductService
{
    public async Task<PagedResult<ProductResponse>> ListAsync(ProductQuery query, CancellationToken cancellationToken)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        PagedResult<Product> page = await repository.ListAsync(query, cancellationToken);
        return new(page.Items.Select(Map).ToList(), page.PageNumber, page.PageSize, page.TotalCount);
    }

    public async Task<ProductResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await repository.GetAsync(id, false, cancellationToken) ?? throw new NotFoundException("Product was not found."));

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        string sku = Product.NormalizeSku(request.Sku);
        if (await repository.SkuExistsAsync(sku, null, cancellationToken))
            throw new ConflictException($"A product with SKU '{sku}' already exists.");
        Product product = new(request.Sku, request.Name, request.Description, DateTimeOffset.UtcNow);
        await repository.AddAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        Product product = await repository.GetAsync(id, true, cancellationToken) ?? throw new NotFoundException("Product was not found.");
        string sku = Product.NormalizeSku(request.Sku);
        if (await repository.SkuExistsAsync(sku, id, cancellationToken))
            throw new ConflictException($"A product with SKU '{sku}' already exists.");
        product.Update(request.Sku, request.Name, request.Description, request.IsActive, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(product);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Product product = await repository.GetAsync(id, true, cancellationToken) ?? throw new NotFoundException("Product was not found.");
        if (await repository.HasInventoryAsync(id, cancellationToken))
            throw new ConflictException("The product cannot be deleted because it has inventory balances.");
        if (await repository.HasMovementsAsync(id, cancellationToken))
            throw new ConflictException("The product cannot be deleted because it has stock movement history.");
        if (await repository.HasTransfersAsync(id, cancellationToken))
            throw new ConflictException("The product cannot be deleted because it is referenced by warehouse transfers.");
        repository.Remove(product);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private static ProductResponse Map(Product x) => new(x.Id, x.Sku, x.Name, x.Description, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
}
