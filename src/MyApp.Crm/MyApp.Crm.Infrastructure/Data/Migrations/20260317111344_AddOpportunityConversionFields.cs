using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Crm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOpportunityConversionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConvertedSalesQuoteId",
                table: "Opportunities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConvertedSalesQuoteNumber",
                table: "Opportunities",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvertedSalesQuoteId",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "ConvertedSalesQuoteNumber",
                table: "Opportunities");
        }
    }
}
