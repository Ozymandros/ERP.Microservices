using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Agentic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSecretKeyNameWithEncryptedApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecretKeyName",
                table: "AIProviders");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedApiKey",
                table: "AIProviders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedApiKey",
                table: "AIProviders");

            migrationBuilder.AddColumn<string>(
                name: "SecretKeyName",
                table: "AIProviders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
