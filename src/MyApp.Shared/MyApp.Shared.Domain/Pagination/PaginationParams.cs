namespace MyApp.Shared.Domain.Pagination;

/// <summary>
/// Pagination parameters for list requests
/// </summary>
public class PaginationParams
{
    /// <summary>
    /// Page number (1-indexed)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Initializes a new instance of the PaginationParams class.
    /// </summary>
    public PaginationParams()
    {
    }

    /// <summary>
    /// Initializes a new instance of the PaginationParams class.
    /// </summary>
    /// <param name="pageNumber">The page Number.</param>
    /// <param name="pageSize">The page Size.</param>
    public PaginationParams(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber > 0 ? pageNumber : 1;
        PageSize = pageSize > 0 ? (pageSize > 100 ? 100 : pageSize) : 10; // Max 100 items per page
    }
}
