using System.Text.Json.Serialization;

namespace MyApp.Shared.Domain.Pagination;

/// <summary>
/// Generic paginated result wrapper for list responses
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Items for the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Current page number (1-indexed)
    /// </summary>
    [JsonPropertyName("page")]
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    [JsonPropertyName("total")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Initializes a new instance of the PaginatedResult class.
    /// </summary>
    public PaginatedResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of the PaginatedResult class.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="pageNumber">The page Number.</param>
    /// <param name="pageSize">The page Size.</param>
    /// <param name="totalCount">The total Count.</param>
    public PaginatedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
