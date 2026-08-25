using FluentValidation;

namespace InventoryWarehouseApi.Application.InventoryReservations;

public sealed class CreateInventoryReservationValidator : AbstractValidator<CreateInventoryReservationRequest>
{
    public CreateInventoryReservationValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.WarehouseLocationId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).PrecisionScale(18, 3, false);
        RuleFor(x => x.ReferenceType).Must(x => x is null || x.Trim().Length <= 64)
            .WithMessage("Reference type cannot exceed 64 characters.");
        RuleFor(x => x.ReferenceId).Must(x => x is null || x.Trim().Length <= 128)
            .WithMessage("Reference ID cannot exceed 128 characters.");
        RuleFor(x => x).Must(x => string.IsNullOrWhiteSpace(x.ReferenceType) == string.IsNullOrWhiteSpace(x.ReferenceId))
            .WithMessage("Reference type and reference ID must both be supplied or both be omitted.");
    }
}

public sealed class InventoryReservationHistoryQueryValidator : AbstractValidator<InventoryReservationHistoryQuery>
{
    public InventoryReservationHistoryQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
