using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Agentic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Replace_TenantId_With_OwnerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agents_TenantId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Agents");

            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Agents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_OwnerUserId",
                table: "Agents",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agents_OwnerUserId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Agents");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Agents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_TenantId",
                table: "Agents",
                column: "TenantId");
        }
    }
}
