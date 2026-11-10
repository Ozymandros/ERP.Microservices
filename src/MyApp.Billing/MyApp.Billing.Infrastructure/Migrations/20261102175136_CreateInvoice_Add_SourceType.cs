using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Billing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateInvoice_Add_SourceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Invoices",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "InvoiceLines",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SourceType_SourceId",
                table: "Invoices",
                columns: new[] { "SourceType", "SourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_SourceType_SourceId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Invoices");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "InvoiceLines",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");
        }
    }
}
