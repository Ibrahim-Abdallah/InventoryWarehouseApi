namespace InventoryWarehouseApi.Domain.Entities;

public sealed class Product
{
    private Product() { }

    public Product(string sku, string name, string? description, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        Sku = NormalizeSku(sku);
        Name = name.Trim();
        Description = NormalizeOptional(description);
        IsActive = true;
        CreatedAtUtc = now.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(string sku, string name, string? description, bool isActive, DateTimeOffset now)
    {
        Sku = NormalizeSku(sku);
        Name = name.Trim();
        Description = NormalizeOptional(description);
        IsActive = isActive;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
