using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Application.Authentication;

public static class Permissions
{
    public const string ClaimType = "permission";
    public const string CatalogRead = "catalog.read";
    public const string CatalogManage = "catalog.manage";
    public const string InventoryRead = "inventory.read";
    public const string InventoryOperate = "inventory.operate";
    public const string InventoryAdjust = "inventory.adjust";
    public const string LowStockManage = "lowstock.manage";
    public const string UsersManage = "users.manage";
    public static readonly IReadOnlyList<string> All = [CatalogRead, CatalogManage, InventoryRead, InventoryOperate, InventoryAdjust, LowStockManage, UsersManage];
    public static IReadOnlyList<string> ForRole(UserRole role) => role switch
    {
        UserRole.Admin => All,
        UserRole.InventoryManager => [CatalogRead, CatalogManage, InventoryRead, InventoryOperate, InventoryAdjust, LowStockManage],
        UserRole.WarehouseOperator => [CatalogRead, InventoryRead, InventoryOperate],
        UserRole.Viewer => [CatalogRead, InventoryRead],
        _ => []
    };
}

public static class AuthorizationPolicies
{
    public const string CatalogRead = nameof(CatalogRead);
    public const string CatalogManage = nameof(CatalogManage);
    public const string InventoryRead = nameof(InventoryRead);
    public const string InventoryOperate = nameof(InventoryOperate);
    public const string InventoryAdjust = nameof(InventoryAdjust);
    public const string LowStockManage = nameof(LowStockManage);
    public const string AdminOnly = nameof(AdminOnly);
}
