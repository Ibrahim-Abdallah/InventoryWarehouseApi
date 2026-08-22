using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.UnitTests;

public sealed class CatalogDomainTests
{
    [Fact]
    public void Product_NormalizesAndUpdatesMasterData()
    {
        DateTimeOffset created = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Product product = new(" sku-001 ", " Product ", " description ", created);
        Assert.Equal("SKU-001", product.Sku);
        Assert.Equal("Product", product.Name);
        Assert.True(product.IsActive);
        product.Update(" sku-002 ", " Updated ", " ", false, created.AddHours(1));
        Assert.Equal("SKU-002", product.Sku);
        Assert.Null(product.Description);
        Assert.False(product.IsActive);
        Assert.Equal(created.AddHours(1), product.UpdatedAtUtc);
    }

    [Fact]
    public void Warehouse_NormalizesAndUpdatesMasterData()
    {
        DateTimeOffset created = DateTimeOffset.UtcNow;
        Warehouse warehouse = new(" wh-cairo ", " Cairo ", null, created);
        Assert.Equal("WH-CAIRO", warehouse.Code);
        warehouse.Update(" wh-giza ", " Giza ", " Main ", false, created.AddMinutes(1));
        Assert.Equal("WH-GIZA", warehouse.Code);
        Assert.Equal("Giza", warehouse.Name);
        Assert.False(warehouse.IsActive);
    }
}
