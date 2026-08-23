using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<bool> SkuExistsAsync(string normalizedSku, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> HasInventoryAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<Product>> ListAsync(ProductQuery query, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    void Remove(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IProductService
{
    Task<PagedResult<ProductResponse>> ListAsync(ProductQuery query, CancellationToken cancellationToken);
    Task<ProductResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
