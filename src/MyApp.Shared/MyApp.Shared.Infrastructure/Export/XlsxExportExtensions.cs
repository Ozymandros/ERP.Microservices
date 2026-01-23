using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace MyApp.Shared.Infrastructure.Export
{
    public static class XlsxExportExtensions
    {
        public static byte[] ExportToXlsx<T>(this IEnumerable<T> items)
        {
            var itemList = items.ToList();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            using var stream = new MemoryStream();
            using (var spreadsheet = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
            {
                var workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                var sheets = spreadsheet.WorkbookPart?.Workbook!.AppendChild(new Sheets());
                var sheetName = typeof(T).Name.EndsWith("s") ? typeof(T).Name : typeof(T).Name + "s";
                var sheet = new Sheet
                {
                    Id = spreadsheet.WorkbookPart?.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = sheetName
                };
                sheets?.Append(sheet);

                // Header row
                var headerRow = new Row();
                foreach (var prop in properties)
                {
                    headerRow.AppendChild(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(prop.Name)
                    });
                }
                sheetData.AppendChild(headerRow);

                // Data rows
                foreach (var item in itemList)
                {
                    var row = new Row();
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item, null);
                        row.AppendChild(new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue(value?.ToString() ?? string.Empty)
                        });
                    }
                    sheetData.AppendChild(row);
                }

                // Best-effort auto-size columns
                AutoSizeColumns(sheetData, properties.Length);

                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }
            return stream.ToArray();
        }

        private static void AutoSizeColumns(SheetData sheetData, int columnCount)
        {
            var columns = new Columns();
            for (int i = 0; i < columnCount; i++)
            {
                double maxWidth = 10; // Default width
                foreach (var row in sheetData.Elements<Row>())
                {
                    var cell = row.Elements<Cell>().ElementAtOrDefault(i);
                    if (cell != null && cell.CellValue != null)
                    {
                        var length = cell.CellValue.Text?.Length ?? 0;
                        if (length > maxWidth) maxWidth = length;
                    }
                }
                // Add a little padding
                columns.Append(new Column { Min = (uint)(i + 1), Max = (uint)(i + 1), Width = maxWidth + 2, CustomWidth = true });
            }
            var worksheet = sheetData.Parent as Worksheet;
            if (worksheet != null)
            {
                worksheet.InsertAt(columns, 0);
            }
        }
    }
}
