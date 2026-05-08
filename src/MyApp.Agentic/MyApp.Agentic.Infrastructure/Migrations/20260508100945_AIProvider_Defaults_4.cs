using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Agentic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AIProvider_Defaults_4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Duplicate migration operations removed:
            // These AIProviders defaults were already introduced in
            // 20260507122530_AddProviderModelCatalogAndDefaults.
            // Keep this migration as a no-op to preserve migration history order.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op.
            // Reverting duplicate drops would incorrectly remove columns that belong
            // to the earlier canonical migration:
            // 20260507122530_AddProviderModelCatalogAndDefaults.
        }
    }
}
