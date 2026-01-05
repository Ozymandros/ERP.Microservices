# User Search Endpoint Example

This file demonstrates how to implement a filtered, sorted, and paginated endpoint using the QuerySpec pattern.

## Pattern Overview

The QuerySpec pattern provides a standardized way to handle:
- **Filtering**: Multi-field filtering with dictionary-based criteria
- **Sorting**: Dynamic sorting by property name with ascending/descending
- **Pagination**: Page number and page size with total count metadata
- **Search**: Full-text-like search across multiple fields

## Implementation Steps

### 1. Create a Specification (Domain Layer)

```csharp
// MyApp.Auth.Domain/Specifications/ApplicationUserQuerySpec.cs
public class ApplicationUserQuerySpec : BaseSpecification<ApplicationUser>
{
    public ApplicationUserQuerySpec(QuerySpec query) : base(query) { }

    public override IQueryable<ApplicationUser> Apply(IQueryable<ApplicationUser> query)
    {
        // Apply entity-specific filters
        if (Query.Filters?.TryGetValue("isActive", out var isActiveStr) == true)
        {
            if (bool.TryParse(isActiveStr, out var isActive))
            {
                query = query.Where(u => u.IsActive == isActive);
            }
        }

        // Apply search
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var searchTerm = Query.SearchTerm.ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm))
            );
        }

        // Apply sorting and pagination
        return ApplyPaginationAndSorting(query);
    }
}
```

### 2. Add Repository Query Method

The base `Repository<T, TKey>` already includes:

```csharp
public virtual async Task<PaginatedResult<TEntity>> QueryAsync(ISpecification<TEntity> spec)
{
    var baseQuery = _dbContext.Set<TEntity>().AsQueryable();
    var totalCount = await baseQuery.CountAsync();
    var paginatedQuery = spec.Apply(baseQuery);
    var items = await paginatedQuery.ToListAsync();
    
    return new PaginatedResult<TEntity>(items, 1, items.Count, totalCount);
}
```

### 3. Add Controller Endpoint

```csharp
[HttpGet("search")]
[HasPermission("Users", "Read")]
public async Task<ActionResult> SearchUsers([FromQuery] QuerySpec query)
{
    try
    {
        query.Validate();
        var spec = new ApplicationUserQuerySpec(query);
        var result = await _userRepository.QueryAsync(spec);
        return Ok(result.ToPaginatedResponse(query.Page, query.PageSize));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error searching users");
        return StatusCode(500, new { message = "An error occurred" });
    }
}
```

## Query Parameter Examples

### Basic Pagination
```
GET /api/users/search?page=1&pageSize=20
```

### With Sorting
```
GET /api/users/search?page=1&pageSize=20&sortBy=createdAt&sortDesc=true
```

### With Filters
```
GET /api/users/search?page=1&pageSize=20&sortBy=email&filters[isActive]=true&filters[email]=domain.com
```

### With Search
```
GET /api/users/search?page=1&pageSize=20&searchTerm=john&sortBy=createdAt&sortDesc=true
```

### Complex Example
```
GET /api/users/search?page=2&pageSize=50&sortBy=createdAt&sortDesc=true&filters[isActive]=true&searchTerm=admin
```

## Response Format

```json
{
  "data": [
    { "id": "...", "email": "user@example.com", "firstName": "John", ... }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "total": 150,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

## Extension Methods Available

### QuerySpec Extensions

```csharp
// Validate and normalize query
query.Validate();

// Add a filter
query.WithFilter("isActive", "true");

// Set default sort
query.WithDefaultSorting("createdAt", descending: true);

// Limit max page size
query.WithMaxPageSize(100);

// Chain operations
query
    .WithFilter("isActive", "true")
    .WithDefaultSorting("createdAt", true)
    .WithMaxPageSize(50);
```

## Supported Filters for ApplicationUser

- **isActive** (bool): Filter by active status
- **email** (string): Filter by email substring
- **userName** (string): Filter by username substring
- **isExternalLogin** (bool): Filter by external login

## Sorting Properties

- createdAt
- email
- firstName
- lastName
- userName
- isActive
- isExternalLogin

## Frontend Integration

See `Frontend/QUERY_BUILDER_PATTERN.md` for React/Vite implementation details.
