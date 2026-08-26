using FluentValidation;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.Reporting;

internal static class ReportValidation
{
    public static void Common<T>(AbstractValidator<T> v,string[] sorts) where T:ReportPageQuery
    { v.RuleFor(x=>x.PageNumber).GreaterThan(0);v.RuleFor(x=>x.PageSize).InclusiveBetween(1,100);v.RuleFor(x=>x.SortBy).Must(x=>x is not null&&sorts.Contains(x,StringComparer.OrdinalIgnoreCase)).WithMessage("SortBy is invalid.");v.RuleFor(x=>x.SortDirection).Must(x=>string.Equals(x,"asc",StringComparison.OrdinalIgnoreCase)||string.Equals(x,"desc",StringComparison.OrdinalIgnoreCase)).WithMessage("SortDirection must be asc or desc."); }
    public static void Dates<T>(AbstractValidator<T> v,Func<T,DateTimeOffset?> from,Func<T,DateTimeOffset?> to,Func<T,Guid?> warehouse,Func<T,Guid?> location)
    { v.RuleFor(x=>x).Must(x=>!from(x).HasValue||!to(x).HasValue||from(x)<to(x)).WithMessage("FromUtc must be earlier than ToUtc.");v.RuleFor(x=>x).Must(x=>!location(x).HasValue||warehouse(x).HasValue).WithMessage("WarehouseId is required when WarehouseLocationId is supplied."); }
}
internal sealed class InventorySummaryQueryValidator:AbstractValidator<InventorySummaryQuery>{public InventorySummaryQueryValidator(){ReportValidation.Common(this,["sku","name","onHand","reserved","available"]);RuleFor(x=>x.Search).MaximumLength(128);}}
internal sealed class WarehouseInventoryQueryValidator:AbstractValidator<WarehouseInventoryQuery>{public WarehouseInventoryQueryValidator(){ReportValidation.Common(this,["sku","name","onHand","reserved","available","locations"]);RuleFor(x=>x.Search).MaximumLength(128);}}
internal sealed class StockMovementReportQueryValidator:AbstractValidator<StockMovementReportQuery>{public StockMovementReportQueryValidator(){ReportValidation.Common(this,["occurredAt","sku","quantity","movementType"]);ReportValidation.Dates(this,x=>x.FromUtc,x=>x.ToUtc,x=>x.WarehouseId,x=>x.WarehouseLocationId);RuleFor(x=>x.MovementType).IsInEnum().When(x=>x.MovementType.HasValue);RuleFor(x=>x.ReferenceType).MaximumLength(StockMovement.ReferenceTypeMaxLength);RuleFor(x=>x.ReferenceId).MaximumLength(StockMovement.ReferenceIdMaxLength);}}
internal sealed class LowStockReportQueryValidator:AbstractValidator<LowStockReportQuery>{public LowStockReportQueryValidator(){ReportValidation.Common(this,["shortage","sku","available","threshold","warehouse"]);RuleFor(x=>x.Search).MaximumLength(128);}}
internal sealed class ProductStockHistoryQueryValidator:AbstractValidator<ProductStockHistoryQuery>{public ProductStockHistoryQueryValidator(){ReportValidation.Common(this,["occurredAt"]);ReportValidation.Dates(this,x=>x.FromUtc,x=>x.ToUtc,x=>x.WarehouseId,x=>x.WarehouseLocationId);RuleFor(x=>x.MovementType).IsInEnum().When(x=>x.MovementType.HasValue);}}
