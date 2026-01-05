namespace MyApp.Shared.Domain.Pagination;

/// <summary>
/// Query specification for filtering, sorting, and pagination.
/// This DTO is used to pass query parameters from the API layer to the application layer.
/// </summary>
public class QuerySpec
{
    /// <summary>
    /// Page number (1-indexed). Default: 1
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Max: 100, Default: 20
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Property name to sort by (e.g., "name", "createdAt").
    /// Leave empty for no sorting.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction. True = descending, False = ascending (default).
    /// </summary>
    public bool SortDesc { get; set; } = false;

    /// <summary>
    /// Filters as key-value pairs (e.g., {"status": "active", "category": "books"}).
    /// Implementation depends on the specification for the entity.
    /// </summary>
    public Dictionary<string, string>? Filters { get; set; }

    /// <summary>
    /// Comma-separated search fields (e.g., "name,description").
    /// Used with SearchTerm for full-text-like queries.
    /// </summary>
    public string? SearchFields { get; set; }

    /// <summary>
    /// Search term to apply across SearchFields.
    /// </summary>
    public string? SearchTerm { get; set; }

    public QuerySpec()
    {
    }

    /// <summary>
    /// Creates a QuerySpec with specified page and page size.
    /// </summary>
    public QuerySpec(int page, int pageSize)
    {
        Page = page > 0 ? page : 1;
        PageSize = pageSize > 0 ? (pageSize > 100 ? 100 : pageSize) : 20;
    }

    /// <summary>
    /// Validate and normalize the query spec.
    /// </summary>
    public void Validate()
    {
        if (Page < 1) Page = 1;
        if (PageSize < 1) PageSize = 20;
        if (PageSize > 100) PageSize = 100;
    }
}
