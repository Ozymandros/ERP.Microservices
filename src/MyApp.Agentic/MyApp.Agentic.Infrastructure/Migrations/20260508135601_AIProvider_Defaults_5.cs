using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Agentic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AIProvider_Defaults_5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_AIModels_ProviderId'
                      AND object_id = OBJECT_ID(N'[AIModels]')
                )
                DROP INDEX [IX_AIModels_ProviderId] ON [AIModels];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIModels', 'CommercialName') IS NULL
                ALTER TABLE [AIModels] ADD [CommercialName] nvarchar(200) NOT NULL CONSTRAINT [DF_AIModels_CommercialName] DEFAULT N'';
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIModels', 'DefaultBotType') IS NULL
                ALTER TABLE [AIModels] ADD [DefaultBotType] nvarchar(20) NOT NULL CONSTRAINT [DF_AIModels_DefaultBotType] DEFAULT N'Chat';
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIModels', 'DefaultEmbeddingDimensions') IS NULL
                ALTER TABLE [AIModels] ADD [DefaultEmbeddingDimensions] int NOT NULL CONSTRAINT [DF_AIModels_DefaultEmbeddingDimensions] DEFAULT 1536;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIModels', 'DefaultEmbeddingModelName') IS NULL
                ALTER TABLE [AIModels] ADD [DefaultEmbeddingModelName] nvarchar(200) NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIModels', 'DefaultEnableMemory') IS NULL
                ALTER TABLE [AIModels] ADD [DefaultEnableMemory] bit NOT NULL CONSTRAINT [DF_AIModels_DefaultEnableMemory] DEFAULT CAST(1 AS bit);
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIModels', 'DefaultEnableRAG') IS NULL
                ALTER TABLE [AIModels] ADD [DefaultEnableRAG] bit NOT NULL CONSTRAINT [DF_AIModels_DefaultEnableRAG] DEFAULT CAST(1 AS bit);
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIModels', 'DefaultMaxTokens') IS NULL
                ALTER TABLE [AIModels] ADD [DefaultMaxTokens] int NOT NULL CONSTRAINT [DF_AIModels_DefaultMaxTokens] DEFAULT 2048;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIModels', 'DefaultSystemPrompt') IS NULL
                ALTER TABLE [AIModels] ADD [DefaultSystemPrompt] nvarchar(max) NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIModels', 'DefaultTemperature') IS NULL
                ALTER TABLE [AIModels] ADD [DefaultTemperature] float(3) NOT NULL CONSTRAINT [DF_AIModels_DefaultTemperature] DEFAULT (0.7);
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIModels', 'DefaultTopK') IS NULL
                ALTER TABLE [AIModels] ADD [DefaultTopK] int NOT NULL CONSTRAINT [DF_AIModels_DefaultTopK] DEFAULT 3;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_AIProviders_Name'
                      AND object_id = OBJECT_ID(N'[AIProviders]')
                )
                CREATE UNIQUE INDEX [IX_AIProviders_Name] ON [AIProviders] ([Name]);
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIProviders', 'DefaultBotType') IS NULL
                ALTER TABLE [AIProviders] ADD [DefaultBotType] nvarchar(20) NOT NULL CONSTRAINT [DF_AIProviders_DefaultBotType] DEFAULT N'Chat';
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIProviders', 'DefaultEmbeddingDimensions') IS NULL
                ALTER TABLE [AIProviders] ADD [DefaultEmbeddingDimensions] int NOT NULL CONSTRAINT [DF_AIProviders_DefaultEmbeddingDimensions] DEFAULT 1536;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIProviders', 'DefaultEmbeddingModelName') IS NULL
                ALTER TABLE [AIProviders] ADD [DefaultEmbeddingModelName] nvarchar(200) NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIProviders', 'DefaultEnableMemory') IS NULL
                ALTER TABLE [AIProviders] ADD [DefaultEnableMemory] bit NOT NULL CONSTRAINT [DF_AIProviders_DefaultEnableMemory] DEFAULT CAST(1 AS bit);
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIProviders', 'DefaultEnableRAG') IS NULL
                ALTER TABLE [AIProviders] ADD [DefaultEnableRAG] bit NOT NULL CONSTRAINT [DF_AIProviders_DefaultEnableRAG] DEFAULT CAST(1 AS bit);
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIProviders', 'DefaultMaxTokens') IS NULL
                ALTER TABLE [AIProviders] ADD [DefaultMaxTokens] int NOT NULL CONSTRAINT [DF_AIProviders_DefaultMaxTokens] DEFAULT 2048;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIProviders', 'DefaultSystemPrompt') IS NULL
                ALTER TABLE [AIProviders] ADD [DefaultSystemPrompt] nvarchar(max) NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIProviders', 'DefaultTemperature') IS NULL
                ALTER TABLE [AIProviders] ADD [DefaultTemperature] float(3) NOT NULL CONSTRAINT [DF_AIProviders_DefaultTemperature] DEFAULT (0.7);
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('AIProviders', 'DefaultTopK') IS NULL
                ALTER TABLE [AIProviders] ADD [DefaultTopK] int NOT NULL CONSTRAINT [DF_AIProviders_DefaultTopK] DEFAULT 3;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_AIModels_ProviderId_CommercialName'
                      AND object_id = OBJECT_ID(N'[AIModels]')
                )
                CREATE UNIQUE INDEX [IX_AIModels_ProviderId_CommercialName] ON [AIModels] ([ProviderId], [CommercialName]);
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_AIModels_ProviderId_TechnicalName'
                      AND object_id = OBJECT_ID(N'[AIModels]')
                )
                CREATE UNIQUE INDEX [IX_AIModels_ProviderId_TechnicalName] ON [AIModels] ([ProviderId], [TechnicalName]);
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
