using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryWarehouseApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseLocationsAndInventoryBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                    table.UniqueConstraint("AK_WarehouseLocations_WarehouseId_Id", x => new { x.WarehouseId, x.Id });
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBalances",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBalances", x => new { x.ProductId, x.WarehouseId, x.WarehouseLocationId });
                    table.CheckConstraint("CK_InventoryBalances_OnHandQuantity_NonNegative", "CAST([OnHandQuantity] AS decimal(18,3)) >= 0");
                    table.CheckConstraint("CK_InventoryBalances_ReservedNotGreaterThanOnHand", "CAST([ReservedQuantity] AS decimal(18,3)) <= CAST([OnHandQuantity] AS decimal(18,3))");
                    table.CheckConstraint("CK_InventoryBalances_ReservedQuantity_NonNegative", "CAST([ReservedQuantity] AS decimal(18,3)) >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBalances_WarehouseLocations_WarehouseId_WarehouseLocationId",
                        columns: x => new { x.WarehouseId, x.WarehouseLocationId },
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_ProductId_WarehouseId",
                table: "InventoryBalances",
                columns: new[] { "ProductId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_WarehouseId_WarehouseLocationId",
                table: "InventoryBalances",
                columns: new[] { "WarehouseId", "WarehouseLocationId" });

            migrationBuilder.CreateIndex(
                name: "UX_WarehouseLocations_WarehouseId_Code",
                table: "WarehouseLocations",
                columns: new[] { "WarehouseId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryBalances");

            migrationBuilder.DropTable(
                name: "WarehouseLocations");
        }
    }
}
