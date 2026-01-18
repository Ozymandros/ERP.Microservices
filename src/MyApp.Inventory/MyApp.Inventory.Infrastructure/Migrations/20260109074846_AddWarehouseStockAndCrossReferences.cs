using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseStockAndCrossReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "InventoryTransactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesOrderId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarehouseStocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "int", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "int", nullable: false),
                    OnOrderQuantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_OrderId",
                table: "InventoryTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_PurchaseOrderId",
                table: "InventoryTransactions",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_SalesOrderId",
                table: "InventoryTransactions",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_ProductId",
                table: "WarehouseStocks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_ProductId_WarehouseId",
                table: "WarehouseStocks",
                columns: new[] { "ProductId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_WarehouseId",
                table: "WarehouseStocks",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_OrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_PurchaseOrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_SalesOrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "SalesOrderId",
                table: "InventoryTransactions");
        }
    }
}
