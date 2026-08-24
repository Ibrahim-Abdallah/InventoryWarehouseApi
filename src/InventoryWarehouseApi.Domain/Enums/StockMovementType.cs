namespace InventoryWarehouseApi.Domain.Enums;

public enum StockMovementType
{
    StockIn = 1,
    StockOut = 2,
    AdjustmentIncrease = 3,
    AdjustmentDecrease = 4,
    TransferOut = 5,
    TransferIn = 6
}
