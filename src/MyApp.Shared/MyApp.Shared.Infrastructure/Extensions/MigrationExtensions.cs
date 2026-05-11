//namespace MyApp.Shared.Infrastructure.Extensions;

//using Microsoft.EntityFrameworkCore.Migrations;
//using Microsoft.EntityFrameworkCore.Migrations.Operations;
//using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

//public static class MigrationExtensions
//{
//    /// <summary>
//    ///     Builds a <see cref="CreateTableOperation" /> to create a new table.
//    /// </summary>
//    /// <remarks>
//    ///     See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see> for more information and examples.
//    /// </remarks>
//    /// <typeparam name="TColumns">Type of a typically anonymous type for building columns.</typeparam>
//    /// <param name="name">The name of the table.</param>
//    /// <param name="columns">
//    ///     A delegate using a <see cref="ColumnsBuilder" /> to create an anonymous type configuring the columns of the table.
//    /// </param>
//    /// <param name="schema">The schema that contains the table, or <see langword="null" /> to use the default schema.</param>
//    /// <param name="constraints">
//    ///     A delegate allowing constraints to be applied over the columns configured by the 'columns' delegate above.
//    /// </param>
//    /// <param name="comment">A comment to be applied to the table.</param>
//    /// <returns>A <see cref="CreateTableBuilder{TColumns}" /> to allow further configuration to be chained.</returns>
//    public static void CreateTableIfNotExists<TColumns>(
//        this MigrationBuilder migrationBuilder,
//        string name,
//        Func<ColumnsBuilder, TColumns> columns,
//        string? schema = null,
//        Action<CreateTableBuilder<TColumns>>? constraints = null,
//        string? comment = null)
//    {
//        var fullTableName = string.IsNullOrEmpty(schema) ? $"[{name}]" : $"[{schema}].[{name}]";

//        // 1. Injectem un SQL que obre un bloc IF en cas que la taula NO existeixi.
//        // Fem servir un 'EXEC' amb una cadena oberta perquè SQL Server no s'embulli amb la sintaxi.
//        migrationBuilder.Sql($@"
//            IF OBJECT_ID(N'{fullTableName}', N'U') IS NULL
//            BEGIN
//                -- El següent codi de creació s'executarà dins d'aquest IF
//        ");

//        // 2. Cridem al mètode original d'EF Core. 
//        // Això afegirà tota la definició de columnes i claus tal com ho fa sempre.
//        migrationBuilder.CreateTable(
//            name: name,
//            schema: schema,
//            columns: columns,
//            constraints: constraints
//        );

//        // 3. Tanquem el bloc condicional 'BEGIN / END' que hem obert al principi.
//        migrationBuilder.Sql("END");
//    }

//    /// <summary>
//    /// Creates a new index on the specified table if the index does not already exist.
//    /// </summary>
//    /// <remarks>This method checks for the existence of both the table and the index before attempting to
//    /// create the index, preventing errors during repeated migrations. Use this method to ensure idempotent index
//    /// creation in custom migration scripts.</remarks>
//    /// <param name="migrationBuilder">The migration builder used to construct database schema changes.</param>
//    /// <param name="name">The name of the index to create.</param>
//    /// <param name="table">The name of the table on which to create the index.</param>
//    /// <param name="column">The name of the column to include in the index. For composite indexes, use an array of column names.</param>
//    /// <param name="schema">The schema that contains the table, or null to use the default schema.</param>
//    /// <param name="unique">true to create a unique index; otherwise, false.</param>
//    /// <param name="filter">An optional filter expression to create a filtered index. If null, no filter is applied.</param>
//    /// <param name="descending">An optional array indicating whether each column in the index should be sorted in descending order. If null,
//    /// columns are sorted in ascending order by default.</param>
//    public static void CreateIndexIfNotExists(
//        this MigrationBuilder migrationBuilder,
//        string name,
//        string table,
//        string column, // o string[] columns si és compost
//        string schema = null,
//        bool unique = false,
//        string? filter = null,
//        bool[]? descending = null)
//    {
//        var fullTableName = string.IsNullOrEmpty(schema) ? $"[{table}]" : $"[{schema}].[{table}]";

//        // 1. Obrim el condicional: Comprovem si la TAULA existeix i si l'ÍNDEX NO existeix
//        migrationBuilder.Sql($@"
//            IF OBJECT_ID(N'{fullTableName}', N'U') IS NOT NULL 
//               AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'{name}' AND object_id = OBJECT_ID(N'{fullTableName}'))
//            BEGIN
//        ");

//        // 2. Cridem al mètode original d'EF Core
//        migrationBuilder.CreateIndex(name, table, column, schema, unique);


//        // 3. Tanquem el bloc
//        migrationBuilder.Sql("END");
//    }

//    /// <summary>
//    ///     Builds a <see cref="CreateIndexOperation" /> to create a new composite (multi-column) index.
//    /// </summary>
//    /// <remarks>
//    ///     See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see> for more information and examples.
//    /// </remarks>
//    /// <param name="name">The index name.</param>
//    /// <param name="table">The table that contains the index.</param>
//    /// <param name="columns">The ordered list of columns that are indexed.</param>
//    /// <param name="schema">The schema that contains the table, or <see langword="null" /> to use the default schema.</param>
//    /// <param name="unique">Indicates whether or not the index enforces uniqueness.</param>
//    /// <param name="filter">The filter to apply to the index, or <see langword="null" /> for no filter.</param>
//    /// <param name="descending">
//    ///     A set of values indicating whether each corresponding index column has descending sort order.
//    ///     If <see langword="null" />, all columns will have ascending order.
//    /// </param>
//    /// <returns>A builder to allow annotations to be added to the operation.</returns>
//    public static OperationBuilder<CreateIndexOperation> CreateIndexIfNotExists(
//        this MigrationBuilder migrationBuilder,
//        string name,
//        string table,
//        string[] columns,
//        string? schema = null,
//        bool unique = false,
//        string? filter = null,
//        bool[]? descending = null)
//    {
//        var fullTableName = string.IsNullOrEmpty(schema) ? $"[{table}]" : $"[{schema}].[{table}]";

//        // 1. Obrim el condicional: Comprovem si la TAULA existeix i si l'ÍNDEX NO existeix
//        migrationBuilder.Sql($@"
//            IF OBJECT_ID(N'{fullTableName}', N'U') IS NOT NULL 
//               AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'{name}' AND object_id = OBJECT_ID(N'{fullTableName}'))
//            BEGIN
//        ");

//        // 2. Cridem al mètode original d'EF Core
//        var result = migrationBuilder.CreateIndex(name, table, columns, schema, unique);

//        // 3. Tanquem el bloc
//        migrationBuilder.Sql("END");
        
//        return result;
//    }

//}
