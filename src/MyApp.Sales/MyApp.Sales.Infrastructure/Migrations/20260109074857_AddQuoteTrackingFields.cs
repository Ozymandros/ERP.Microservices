using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Sales.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConvertedToOrderId",
                table: "SalesOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsQuote",
                table: "SalesOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuoteExpiryDate",
                table: "SalesOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "SalesOrderLines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductSKU",
                table: "SalesOrderLines",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvertedToOrderId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "IsQuote",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "QuoteExpiryDate",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "ProductSKU",
                table: "SalesOrderLines");
        }
    }
}
