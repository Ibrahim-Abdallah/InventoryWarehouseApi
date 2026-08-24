using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryWarehouseApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_MovementType_Supported",
                table: "StockMovements");

            migrationBuilder.CreateTable(
                name: "WarehouseTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceWarehouseLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationWarehouseLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransfers", x => x.Id);
                    table.CheckConstraint("CK_WarehouseTransfers_DifferentPositions", "[SourceWarehouseId] <> [DestinationWarehouseId] OR [SourceWarehouseLocationId] <> [DestinationWarehouseLocationId]");
                    table.CheckConstraint("CK_WarehouseTransfers_Status_Supported", "[Status] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_WarehouseLocations_DestinationWarehouseId_DestinationWarehouseLocationId",
                        columns: x => new { x.DestinationWarehouseId, x.DestinationWarehouseLocationId },
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_WarehouseLocations_SourceWarehouseId_SourceWarehouseLocationId",
                        columns: x => new { x.SourceWarehouseId, x.SourceWarehouseLocationId },
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransferItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseTransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    TransferOutMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransferInMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransferItems", x => x.Id);
                    table.CheckConstraint("CK_WarehouseTransferItems_MovementPair", "([TransferOutMovementId] IS NULL AND [TransferInMovementId] IS NULL) OR ([TransferOutMovementId] IS NOT NULL AND [TransferInMovementId] IS NOT NULL)");
                    table.CheckConstraint("CK_WarehouseTransferItems_Quantity_Positive", "CAST([Quantity] AS decimal(18,3)) > 0");
                    table.ForeignKey(
                        name: "FK_WarehouseTransferItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferItems_StockMovements_TransferInMovementId",
                        column: x => x.TransferInMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferItems_StockMovements_TransferOutMovementId",
                        column: x => x.TransferOutMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferItems_WarehouseTransfers_WarehouseTransferId",
                        column: x => x.WarehouseTransferId,
                        principalTable: "WarehouseTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_MovementType_Supported",
                table: "StockMovements",
                sql: "[MovementType] IN (1, 2, 3, 4, 5, 6)");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferItems_ProductId",
                table: "WarehouseTransferItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "UX_WarehouseTransferItems_Transfer_Product",
                table: "WarehouseTransferItems",
                columns: new[] { "WarehouseTransferId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_WarehouseTransferItems_TransferInMovementId",
                table: "WarehouseTransferItems",
                column: "TransferInMovementId",
                unique: true,
                filter: "[TransferInMovementId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_WarehouseTransferItems_TransferOutMovementId",
                table: "WarehouseTransferItems",
                column: "TransferOutMovementId",
                unique: true,
                filter: "[TransferOutMovementId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_CreatedAtUtc_Id",
                table: "WarehouseTransfers",
                columns: new[] { "CreatedAtUtc", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_DestinationWarehouseId_DestinationWarehouseLocationId",
                table: "WarehouseTransfers",
                columns: new[] { "DestinationWarehouseId", "DestinationWarehouseLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_SourceWarehouseId_SourceWarehouseLocationId",
                table: "WarehouseTransfers",
                columns: new[] { "SourceWarehouseId", "SourceWarehouseLocationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseTransferItems");

            migrationBuilder.DropTable(
                name: "WarehouseTransfers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_MovementType_Supported",
                table: "StockMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_MovementType_Supported",
                table: "StockMovements",
                sql: "[MovementType] IN (1, 2, 3, 4)");
        }
    }
}
