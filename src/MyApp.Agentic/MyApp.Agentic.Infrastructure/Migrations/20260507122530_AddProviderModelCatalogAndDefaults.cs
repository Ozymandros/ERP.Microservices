using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Agentic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderModelCatalogAndDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AIModels_ProviderId",
                table: "AIModels");

            migrationBuilder.AddColumn<string>(
                name: "CommercialName",
                table: "AIModels",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultBotType",
                table: "AIModels",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Chat");

            migrationBuilder.AddColumn<int>(
                name: "DefaultEmbeddingDimensions",
                table: "AIModels",
                type: "int",
                nullable: false,
                defaultValue: 1536);

            migrationBuilder.AddColumn<string>(
                name: "DefaultEmbeddingModelName",
                table: "AIModels",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DefaultEnableMemory",
                table: "AIModels",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "DefaultEnableRAG",
                table: "AIModels",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultMaxTokens",
                table: "AIModels",
                type: "int",
                nullable: false,
                defaultValue: 2048);

            migrationBuilder.AddColumn<string>(
                name: "DefaultSystemPrompt",
                table: "AIModels",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DefaultTemperature",
                table: "AIModels",
                type: "float(3)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0.69999999999999996);

            migrationBuilder.AddColumn<int>(
                name: "DefaultTopK",
                table: "AIModels",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateIndex(
                name: "IX_AIProviders_Name",
                table: "AIProviders",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_ProviderId_CommercialName",
                table: "AIModels",
                columns: new[] { "ProviderId", "CommercialName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_ProviderId_TechnicalName",
                table: "AIModels",
                columns: new[] { "ProviderId", "TechnicalName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AIProviders_Name",
                table: "AIProviders");

            migrationBuilder.DropIndex(
                name: "IX_AIModels_ProviderId_CommercialName",
                table: "AIModels");

            migrationBuilder.DropIndex(
                name: "IX_AIModels_ProviderId_TechnicalName",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "CommercialName",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "DefaultBotType",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "DefaultEmbeddingDimensions",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "DefaultEmbeddingModelName",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "DefaultEnableMemory",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "DefaultEnableRAG",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "DefaultMaxTokens",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "DefaultSystemPrompt",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "DefaultTemperature",
                table: "AIModels");

            migrationBuilder.DropColumn(
                name: "DefaultTopK",
                table: "AIModels");

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_ProviderId",
                table: "AIModels",
                column: "ProviderId");
        }
    }
}
