using FluentValidation;

namespace InventoryWarehouseApi.Application.WarehouseTransfers;

public sealed class CreateWarehouseTransferValidator : AbstractValidator<CreateWarehouseTransferRequest>
{
    public CreateWarehouseTransferValidator()
    {
        RuleFor(x => x.SourceWarehouseId).NotEmpty();
        RuleFor(x => x.SourceWarehouseLocationId).NotEmpty();
        RuleFor(x => x.DestinationWarehouseId).NotEmpty();
        RuleFor(x => x.DestinationWarehouseLocationId).NotEmpty();
        RuleFor(x => x).Must(x => x.SourceWarehouseId != x.DestinationWarehouseId ||
            x.SourceWarehouseLocationId != x.DestinationWarehouseLocationId)
            .WithMessage("Source and destination positions must differ.");
        RuleFor(x => x.Items).NotNull().Must(x => x is { Count: >= 1 and <= 100 })
            .WithMessage("Items must contain between 1 and 100 entries.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0).PrecisionScale(18, 3, false);
        });
        RuleFor(x => x.Items).Must(x => x is null || x.Select(i => i.ProductId).Distinct().Count() == x.Count)
            .WithMessage("Duplicate products are not allowed in a warehouse transfer.");
    }
}

public sealed class WarehouseTransferHistoryQueryValidator : AbstractValidator<WarehouseTransferHistoryQuery>
{
    public WarehouseTransferHistoryQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
