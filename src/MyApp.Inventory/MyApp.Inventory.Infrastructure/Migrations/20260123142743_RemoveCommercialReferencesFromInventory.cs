using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCommercialReferencesFromInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_PurchaseOrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_SalesOrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "SalesOrderId",
                table: "InventoryTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesOrderId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_PurchaseOrderId",
                table: "InventoryTransactions",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_SalesOrderId",
                table: "InventoryTransactions",
                column: "SalesOrderId");
        }
    }
}
