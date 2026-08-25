using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryWarehouseApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLowStockMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LowStockThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThresholdQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LowStockThresholds", x => x.Id);
                    table.CheckConstraint("CK_LowStockThresholds_ThresholdQuantity_NonNegative", "[ThresholdQuantity] >= 0");
                    table.CheckConstraint("CK_LowStockThresholds_Timestamps", "[UpdatedAtUtc] >= [CreatedAtUtc]");
                    table.ForeignKey(
                        name: "FK_LowStockThresholds_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LowStockThresholds_WarehouseLocations_WarehouseId_WarehouseLocationId",
                        columns: x => new { x.WarehouseId, x.WarehouseLocationId },
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LowStockThresholds_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LowStockAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LowStockThresholdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThresholdQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    TriggeredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LowStockAlerts", x => x.Id);
                    table.CheckConstraint("CK_LowStockAlerts_AvailableQuantity_NonNegative", "[AvailableQuantity] >= 0");
                    table.CheckConstraint("CK_LowStockAlerts_ObservationTimestamp", "[LastObservedAtUtc] >= [TriggeredAtUtc]");
                    table.CheckConstraint("CK_LowStockAlerts_ResolutionTimestamp", "[ResolvedAtUtc] IS NULL OR [ResolvedAtUtc] >= [TriggeredAtUtc]");
                    table.CheckConstraint("CK_LowStockAlerts_ThresholdQuantity_NonNegative", "[ThresholdQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_LowStockAlerts_LowStockThresholds_LowStockThresholdId",
                        column: x => x.LowStockThresholdId,
                        principalTable: "LowStockThresholds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LowStockAlerts_TriggeredAtUtc_Id",
                table: "LowStockAlerts",
                columns: new[] { "TriggeredAtUtc", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "UX_LowStockAlerts_ActiveThreshold",
                table: "LowStockAlerts",
                column: "LowStockThresholdId",
                unique: true,
                filter: "[ResolvedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LowStockThresholds_IsEnabled_WarehouseId_ProductId",
                table: "LowStockThresholds",
                columns: new[] { "IsEnabled", "WarehouseId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_LowStockThresholds_WarehouseId_WarehouseLocationId",
                table: "LowStockThresholds",
                columns: new[] { "WarehouseId", "WarehouseLocationId" });

            migrationBuilder.CreateIndex(
                name: "UX_LowStockThresholds_Position",
                table: "LowStockThresholds",
                columns: new[] { "ProductId", "WarehouseId", "WarehouseLocationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LowStockAlerts");

            migrationBuilder.DropTable(
                name: "LowStockThresholds");
        }
    }
}
