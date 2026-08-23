using InventoryWarehouseApi.Application.Common;

namespace InventoryWarehouseApi.Application.Inventory;

public sealed class LocationInventoryQueryValidator : PagedQueryValidator<LocationInventoryQuery>
{
    public LocationInventoryQueryValidator() : base(["code", "name"]) { }
}
