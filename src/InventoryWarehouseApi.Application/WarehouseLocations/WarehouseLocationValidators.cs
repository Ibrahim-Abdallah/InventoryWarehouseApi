using FluentValidation;
using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.WarehouseLocations;

public sealed class CreateWarehouseLocationValidator : AbstractValidator<CreateWarehouseLocationRequest>
{
    public CreateWarehouseLocationValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
public sealed class UpdateWarehouseLocationValidator : AbstractValidator<UpdateWarehouseLocationRequest>
{
    public UpdateWarehouseLocationValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
public sealed class WarehouseLocationQueryValidator : PagedQueryValidator<WarehouseLocationQuery>
{
    public WarehouseLocationQueryValidator() : base(["code", "name", "createdAtUtc", "updatedAtUtc"]) { }
}
