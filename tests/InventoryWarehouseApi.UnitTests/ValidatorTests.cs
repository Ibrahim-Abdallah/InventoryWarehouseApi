using FluentValidation;
using InventoryWarehouseApi.Application.Products;
using InventoryWarehouseApi.Application.Warehouses;

namespace InventoryWarehouseApi.UnitTests;

public sealed class ValidatorTests
{
    [Fact]
    public void ProductValidator_RejectsMissingAndOversizedFields()
    {
        var result = new CreateProductValidator().Validate(new CreateProductRequest("", "", new string('x', 1001)));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Sku");
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
        Assert.Contains(result.Errors, x => x.PropertyName == "Description");
    }

    [Fact]
    public void WarehouseValidator_RejectsMissingAndOversizedFields()
    {
        var result = new CreateWarehouseValidator().Validate(new CreateWarehouseRequest("", "", new string('x', 1001)));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Code");
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
    }

    [Theory]
    [InlineData(0, 20, "sku", "asc")]
    [InlineData(1, 101, "sku", "asc")]
    [InlineData(1, 20, "unknown", "asc")]
    [InlineData(1, 20, "sku", "sideways")]
    public void ProductQueryValidator_RejectsInvalidPagingOrSorting(int page, int size, string sort, string direction)
    {
        Assert.False(new ProductQueryValidator().Validate(new ProductQuery(page, size, SortBy: sort, SortDirection: direction)).IsValid);
    }

    [Fact]
    public void ProductQueryValidator_RejectsNullSortDirectionWithoutThrowing()
    {
        var result = new ProductQueryValidator().Validate(new ProductQuery(SortDirection: null!));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "SortDirection");
    }
}
