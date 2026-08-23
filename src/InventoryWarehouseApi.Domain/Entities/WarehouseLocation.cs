namespace InventoryWarehouseApi.Domain.Entities;

public sealed class WarehouseLocation
{
    private WarehouseLocation() { }

    public WarehouseLocation(Guid warehouseId, string code, string name, string? description, DateTimeOffset now)
    {
        if (warehouseId == Guid.Empty) throw new ArgumentException("Warehouse is required.", nameof(warehouseId));
        Id = Guid.NewGuid();
        WarehouseId = warehouseId;
        Code = NormalizeCode(code);
        Name = name.Trim();
        Description = NormalizeOptional(description);
        IsActive = true;
        CreatedAtUtc = now.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(string code, string name, string? description, bool isActive, DateTimeOffset now)
    {
        Code = NormalizeCode(code);
        Name = name.Trim();
        Description = NormalizeOptional(description);
        IsActive = isActive;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
