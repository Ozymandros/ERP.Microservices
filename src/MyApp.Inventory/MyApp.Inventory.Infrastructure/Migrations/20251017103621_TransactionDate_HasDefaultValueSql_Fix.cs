using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TransactionDate_HasDefaultValueSql_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TransactionDate",
                table: "InventoryTransactions",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 10, 16, 8, 39, 1, 302, DateTimeKind.Utc).AddTicks(9472));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TransactionDate",
                table: "InventoryTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 10, 16, 8, 39, 1, 302, DateTimeKind.Utc).AddTicks(9472),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");
        }
    }
}
