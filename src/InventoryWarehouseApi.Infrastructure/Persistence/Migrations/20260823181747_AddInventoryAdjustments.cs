using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryWarehouseApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_MovementType_Supported",
                table: "StockMovements");

            migrationBuilder.CreateTable(
                name: "InventoryAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdjustedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdjustedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAdjustments", x => x.Id);
                    table.CheckConstraint("CK_InventoryAdjustments_AdjustmentType_Supported", "[AdjustmentType] IN (1, 2)");
                    table.CheckConstraint("CK_InventoryAdjustments_Quantity_Positive", "CAST([Quantity] AS decimal(18,3)) > 0");
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_WarehouseLocations_WarehouseId_WarehouseLocationId",
                        columns: x => new { x.WarehouseId, x.WarehouseLocationId },
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_MovementType_Supported",
                table: "StockMovements",
                sql: "[MovementType] IN (1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_Position_AdjustedAtUtc",
                table: "InventoryAdjustments",
                columns: new[] { "ProductId", "WarehouseId", "WarehouseLocationId", "AdjustedAtUtc", "Id" },
                descending: new[] { false, false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_StockMovementId",
                table: "InventoryAdjustments",
                column: "StockMovementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_WarehouseId_WarehouseLocationId",
                table: "InventoryAdjustments",
                columns: new[] { "WarehouseId", "WarehouseLocationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryAdjustments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_MovementType_Supported",
                table: "StockMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_MovementType_Supported",
                table: "StockMovements",
                sql: "[MovementType] IN (1, 2)");
        }
    }
}
