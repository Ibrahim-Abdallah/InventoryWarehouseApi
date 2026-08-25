using FluentValidation;
namespace InventoryWarehouseApi.Application.LowStock;
internal sealed class UpsertLowStockThresholdValidator:AbstractValidator<UpsertLowStockThresholdRequest>{public UpsertLowStockThresholdValidator()=>RuleFor(x=>x.ThresholdQuantity).GreaterThanOrEqualTo(0).PrecisionScale(18,3,false);}
internal sealed class LowStockThresholdQueryValidator:AbstractValidator<LowStockThresholdQuery>{public LowStockThresholdQueryValidator(){RuleFor(x=>x.PageNumber).GreaterThan(0);RuleFor(x=>x.PageSize).InclusiveBetween(1,100);RuleFor(x=>x.ProductId).NotEqual(Guid.Empty).When(x=>x.ProductId.HasValue);RuleFor(x=>x.WarehouseId).NotEqual(Guid.Empty).When(x=>x.WarehouseId.HasValue);}}
internal sealed class LowStockQueryValidator:AbstractValidator<LowStockQuery>{public LowStockQueryValidator(){RuleFor(x=>x.PageNumber).GreaterThan(0);RuleFor(x=>x.PageSize).InclusiveBetween(1,100);}}
internal sealed class LowStockAlertQueryValidator:AbstractValidator<LowStockAlertQuery>{public LowStockAlertQueryValidator(){RuleFor(x=>x.PageNumber).GreaterThan(0);RuleFor(x=>x.PageSize).InclusiveBetween(1,100);}}
