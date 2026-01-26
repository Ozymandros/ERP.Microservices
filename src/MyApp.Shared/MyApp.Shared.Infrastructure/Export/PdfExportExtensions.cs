using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MyApp.Shared.Infrastructure.Export
{
    public static class PdfExportExtensions
    {
        static PdfExportExtensions()
        {
            // Configure QuestPDF license for Community use
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Exports a collection of items to a PDF document with professional formatting
        /// </summary>
        /// <typeparam name="T">The type of items to export</typeparam>
        /// <param name="items">Collection of items to export</param>
        /// <returns>PDF document as byte array</returns>
        public static byte[] ExportToPdf<T>(this IEnumerable<T> items)
        {
            var itemList = items.ToList();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var typeName = typeof(T).Name.EndsWith("Dto") 
                ? typeof(T).Name.Substring(0, typeof(T).Name.Length - 3) 
                : typeof(T).Name;
            var pluralName = typeName.EndsWith("s") ? typeName : typeName + "s";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(40);
                    page.PageColor(Colors.White);

                    // Header
                    page.Header().Element(c => ComposeHeader(c, pluralName));

                    // Content with table
                    page.Content().Element(c => ComposeContent(c, itemList, properties));

                    // Footer
                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void ComposeHeader(IContainer container, string entityName)
        {
            container.Column(column =>
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.DefaultTextStyle(TextStyle.Default.FontSize(20).Bold().FontColor(Colors.Blue.Darken2));
                            text.Span($"{entityName} Export Report");
                        });

                        col.Item().Text(text =>
                        {
                            text.DefaultTextStyle(TextStyle.Default.FontSize(10).FontColor(Colors.Grey.Darken1));
                            text.Span($"Generated On: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                        });
                    });
                });

                column.Item().PaddingTop(10);
            });
        }

        private static void ComposeContent<T>(IContainer container, List<T> items, PropertyInfo[] properties)
        {
            container.Table(table =>
            {
                // Define columns based on properties
                table.ColumnsDefinition(columns =>
                {
                    foreach (var prop in properties)
                    {
                        // Special handling for common property types
                        if (prop.Name == "Id")
                            columns.ConstantColumn(60);
                        else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                            columns.ConstantColumn(100);
                        else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?) ||
                                 prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?))
                            columns.ConstantColumn(80);
                        else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                            columns.ConstantColumn(60);
                        else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                            columns.ConstantColumn(70);
                        else
                            columns.RelativeColumn();
                    }
                });

                // Header row
                table.Header(header =>
                {
                    foreach (var prop in properties)
                    {
                        header.Cell()
                            .Background(Colors.Grey.Lighten2)
                            .BorderBottom(1)
                            .BorderColor(Colors.Grey.Medium)
                            .Padding(8)
                            .Text(text =>
                            {
                                text.DefaultTextStyle(TextStyle.Default.Bold().FontSize(10));
                                text.Span(prop.Name);
                            });
                    }
                });

                // Data rows
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var isEvenRow = i % 2 == 0;

                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item, null);
                        var formattedValue = FormatValue(value, prop.PropertyType);

                        table.Cell()
                            .Background(isEvenRow ? Colors.White : Colors.Grey.Lighten3)
                            .BorderBottom(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(8)
                            .Text(text =>
                            {
                                text.DefaultTextStyle(TextStyle.Default.FontSize(9));
                                text.Span(formattedValue);
                            });
                    }
                }
            });
        }

        private static string FormatValue(object? value, Type propertyType)
        {
            if (value == null)
                return string.Empty;

            // Currency formatting for decimal types
            if (propertyType == typeof(decimal) || propertyType == typeof(decimal?))
            {
                return ((decimal)value).ToString("C2");
            }

            if (propertyType == typeof(double) || propertyType == typeof(double?))
            {
                return ((double)value).ToString("C2");
            }

            // Date formatting
            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                return ((DateTime)value).ToString("yyyy-MM-dd HH:mm");
            }

            // Boolean formatting
            if (propertyType == typeof(bool) || propertyType == typeof(bool?))
            {
                return (bool)value ? "Yes" : "No";
            }

            return value.ToString() ?? string.Empty;
        }
    }
}
