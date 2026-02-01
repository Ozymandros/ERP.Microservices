using Microsoft.AspNetCore.Mvc;

namespace MyApp.Shared.Infrastructure.Export
{
    public abstract class BaseExportController: ControllerBase
    {

        // XLSX export logic is now in XlsxExportExtensions. This base class only provides the FileContentResult helper.

        protected FileContentResult ToXlsxFile<T>(IEnumerable<T> items, string fileName)
        {
            var bytes = items.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
