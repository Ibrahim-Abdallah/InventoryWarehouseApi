using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryWarehouseApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_OccurredAtUtc_Id",
                table: "StockMovements",
                columns: new[] { "OccurredAtUtc", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_OccurredAtUtc_Id",
                table: "StockMovements",
                columns: new[] { "ProductId", "OccurredAtUtc", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WarehouseId_OccurredAtUtc_Id",
                table: "StockMovements",
                columns: new[] { "WarehouseId", "OccurredAtUtc", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_WarehouseId_ProductId_WarehouseLocationId",
                table: "InventoryBalances",
                columns: new[] { "WarehouseId", "ProductId", "WarehouseLocationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockMovements_OccurredAtUtc_Id",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductId_OccurredAtUtc_Id",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_WarehouseId_OccurredAtUtc_Id",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryBalances_WarehouseId_ProductId_WarehouseLocationId",
                table: "InventoryBalances");
        }
    }
}
