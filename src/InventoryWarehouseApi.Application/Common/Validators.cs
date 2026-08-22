using FluentValidation;

namespace InventoryWarehouseApi.Application.Common;

public abstract class PagedQueryValidator<T> : AbstractValidator<T> where T : PagedQuery
{
    protected PagedQueryValidator(IEnumerable<string> sortFields)
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortDirection)
            .NotEmpty()
            .Must(x => string.Equals(x, "asc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, "desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be 'asc' or 'desc'.");
        RuleFor(x => x.SortBy).Must(x => x is null || sortFields.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortBy is not supported.");
    }
}
