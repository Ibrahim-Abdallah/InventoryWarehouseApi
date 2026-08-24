using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.Inventory;

public sealed class LocationInventoryQueryValidator : PagedQueryValidator<LocationInventoryQuery>
{
    public LocationInventoryQueryValidator() : base(["code", "name"]) { }
}

public sealed class StockMovementRequestValidator : AbstractValidator<StockMovementRequest>
{
    public StockMovementRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).PrecisionScale(18, 3, false);
        RuleFor(x => x.ReferenceType).MaximumLength(StockMovement.ReferenceTypeMaxLength);
        RuleFor(x => x.ReferenceId).MaximumLength(StockMovement.ReferenceIdMaxLength);
        RuleFor(x => x).Must(x => HasValue(x.ReferenceType) == HasValue(x.ReferenceId))
            .WithMessage("ReferenceType and ReferenceId must both be supplied or both be omitted.");
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);
}

public sealed class StockMovementHistoryQueryValidator : AbstractValidator<StockMovementHistoryQuery>
{
    public StockMovementHistoryQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class InventoryAdjustmentRequestValidator : AbstractValidator<InventoryAdjustmentRequest>
{
    public InventoryAdjustmentRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).PrecisionScale(18, 3, false);
        RuleFor(x => x.Reason).NotEmpty().Must(x => x is null || x.Trim().Length <= InventoryAdjustment.ReasonMaxLength)
            .WithMessage($"Reason cannot exceed {InventoryAdjustment.ReasonMaxLength} characters after trimming.");
        RuleFor(x => x.AdjustedBy).NotEmpty().Must(x => x is null || x.Trim().Length <= InventoryAdjustment.AdjustedByMaxLength)
            .WithMessage($"AdjustedBy cannot exceed {InventoryAdjustment.AdjustedByMaxLength} characters after trimming.");
    }
}

public sealed class InventoryAdjustmentHistoryQueryValidator : AbstractValidator<InventoryAdjustmentHistoryQuery>
{
    public InventoryAdjustmentHistoryQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
