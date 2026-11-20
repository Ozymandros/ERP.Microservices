using Microsoft.AspNetCore.Mvc;

namespace MyApp.Shared.Infrastructure.Export
{
    /// <summary>
    /// Provides Base Export Controller functionality.
    /// </summary>
    public abstract class BaseExportController : ControllerBase
    {

        // XLSX export logic is now in XlsxExportExtensions. This base class only provides the FileContentResult helper.

        /// <summary>Exports the given items to an XLSX file and returns it as a <see cref="FileContentResult"/>.</summary>
        /// <typeparam name="T">The type of items to export.</typeparam>
        /// <param name="items">The collection of items to export.</param>
        /// <param name="fileName">The suggested file name for the download.</param>
        /// <returns>A <see cref="FileContentResult"/> with the XLSX content type and the given file name.</returns>
        protected FileContentResult ToXlsxFile<T>(IEnumerable<T> items, string fileName)
        {
            var bytes = items.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
