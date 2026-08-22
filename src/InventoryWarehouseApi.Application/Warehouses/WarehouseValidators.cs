using FluentValidation;
using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.Warehouses;

public sealed class CreateWarehouseValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
public sealed class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
public sealed class WarehouseQueryValidator : PagedQueryValidator<WarehouseQuery>
{
    public WarehouseQueryValidator() : base(["code", "name", "createdAtUtc", "updatedAtUtc"]) { }
}
