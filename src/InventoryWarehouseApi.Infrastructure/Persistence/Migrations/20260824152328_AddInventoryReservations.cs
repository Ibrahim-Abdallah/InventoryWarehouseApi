using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryWarehouseApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReferenceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReleasedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FulfilledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FulfillmentMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => x.Id);
                    table.CheckConstraint("CK_InventoryReservations_LifecycleConsistency", "([Status] = 1 AND [ReleasedAtUtc] IS NULL AND [FulfilledAtUtc] IS NULL AND [FulfillmentMovementId] IS NULL) OR\n([Status] = 2 AND [ReleasedAtUtc] IS NOT NULL AND [ReleasedAtUtc] >= [CreatedAtUtc] AND [FulfilledAtUtc] IS NULL AND [FulfillmentMovementId] IS NULL) OR\n([Status] = 3 AND [ReleasedAtUtc] IS NULL AND [FulfilledAtUtc] IS NOT NULL AND [FulfilledAtUtc] >= [CreatedAtUtc] AND [FulfillmentMovementId] IS NOT NULL)");
                    table.CheckConstraint("CK_InventoryReservations_Quantity_Positive", "CAST([Quantity] AS decimal(18,3)) > 0");
                    table.CheckConstraint("CK_InventoryReservations_ReferencePair", "([ReferenceType] IS NULL AND [ReferenceId] IS NULL) OR ([ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL)");
                    table.CheckConstraint("CK_InventoryReservations_Status_Supported", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_StockMovements_FulfillmentMovementId",
                        column: x => x.FulfillmentMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_WarehouseLocations_WarehouseId_WarehouseLocationId",
                        columns: x => new { x.WarehouseId, x.WarehouseLocationId },
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_CreatedAtUtc_Id",
                table: "InventoryReservations",
                columns: new[] { "CreatedAtUtc", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_FulfillmentMovementId",
                table: "InventoryReservations",
                column: "FulfillmentMovementId",
                unique: true,
                filter: "[FulfillmentMovementId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_Position_CreatedAtUtc_Id",
                table: "InventoryReservations",
                columns: new[] { "ProductId", "WarehouseId", "WarehouseLocationId", "CreatedAtUtc", "Id" },
                descending: new[] { false, false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ReferenceType_ReferenceId",
                table: "InventoryReservations",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_WarehouseId_WarehouseLocationId",
                table: "InventoryReservations",
                columns: new[] { "WarehouseId", "WarehouseLocationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryReservations");
        }
    }
}
