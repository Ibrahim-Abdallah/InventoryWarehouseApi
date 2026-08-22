using FluentValidation;
using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.Products;

public sealed class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
public sealed class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
public sealed class ProductQueryValidator : PagedQueryValidator<ProductQuery>
{
    public ProductQueryValidator() : base(["sku", "name", "createdAtUtc", "updatedAtUtc"]) { }
}
