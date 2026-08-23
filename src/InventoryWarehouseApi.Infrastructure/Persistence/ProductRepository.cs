using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryWarehouseApi.Infrastructure.Persistence;

internal sealed class ProductRepository(InventoryWarehouseDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetAsync(Guid id, bool tracking, CancellationToken ct) =>
        (tracking ? dbContext.Products : dbContext.Products.AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> SkuExistsAsync(string sku, Guid? excludingId, CancellationToken ct) =>
        dbContext.Products.AnyAsync(x => x.Sku == sku && (!excludingId.HasValue || x.Id != excludingId), ct);
    public Task<bool> HasInventoryAsync(Guid id, CancellationToken ct) => dbContext.InventoryBalances.AnyAsync(x => x.ProductId == id, ct);
    public async Task<PagedResult<Product>> ListAsync(ProductQuery q, CancellationToken ct)
    {
        IQueryable<Product> query = dbContext.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            string search = q.Search.Trim();
            query = query.Where(x => x.Sku.Contains(search) || x.Name.Contains(search));
        }
        if (q.IsActive.HasValue) query = query.Where(x => x.IsActive == q.IsActive);
        int count = await query.CountAsync(ct);
        bool desc = q.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        query = (q.SortBy?.ToLowerInvariant(), desc) switch
        {
            ("sku", false) => query.OrderBy(x => x.Sku).ThenBy(x => x.Id), ("sku", true) => query.OrderByDescending(x => x.Sku).ThenBy(x => x.Id),
            ("name", false) => query.OrderBy(x => x.Name).ThenBy(x => x.Id), ("name", true) => query.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
            ("updatedatutc", false) => query.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Id), ("updatedatutc", true) => query.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Id),
            ("createdatutc", false) => query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
        };
        return new(await query.Skip((q.PageNumber - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct), q.PageNumber, q.PageSize, count);
    }
    public Task AddAsync(Product product, CancellationToken ct) => dbContext.Products.AddAsync(product, ct).AsTask();
    public void Remove(Product product) => dbContext.Products.Remove(product);
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try { await dbContext.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (UniqueConstraintDetector.Matches(ex, "UX_Products_Sku", "Products.Sku"))
        { throw new ConflictException("A product with this SKU already exists."); }
        catch (DbUpdateException ex) when (UniqueConstraintDetector.Matches(ex, "FK_InventoryBalances_Products", "FOREIGN KEY constraint failed"))
        { throw new ConflictException("The product cannot be deleted because it has inventory balances."); }
    }
}
