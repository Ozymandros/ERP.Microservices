# Query Spec Pattern: Comprehensive Architecture Guide

## Overview

This document describes the **Query Specification Pattern** implemented across your hexagonal architecture for filtering, sorting, and pagination. It provides a **modular, scalable, and reusable** approach to handle complex list/search queries in both backend and frontend.

---

## 🏗️ Architecture Layers

### 1. **Domain Layer** (Core Business Logic)

**Location**: `src/MyApp.Shared/MyApp.Shared.Domain/`

#### Key Components

- **`QuerySpec`** (`Pagination/QuerySpec.cs`)
  - DTO representing query parameters from API clients
  - Properties: `page`, `pageSize`, `sortBy`, `sortDesc`, `filters`, `searchTerm`, `searchFields`
  - Includes validation and normalization logic
  - Max page size: 100 items (security/performance constraint)

- **`ISpecification<T>` & `BaseSpecification<T>`** (`Specifications/ISpecification.cs`)
  - Interface for encapsulating entity-specific query logic
  - Base class with shared pagination and sorting helpers
  - Implementations are entity-specific (e.g., `ApplicationUserQuerySpec`)

- **`DynamicLinqExtensions`** (`Extensions/DynamicLinqExtensions.cs`)
  - Runtime LINQ methods for dynamic sorting and filtering
  - `OrderByDynamic()` - Sort by property name at runtime
  - `FilterByProperty()` - Filter by property equality
  - `FilterByMultipleProperties()` - Search across multiple fields

#### Flow
```
HTTP Query Params
    ↓
QuerySpec (DTO)
    ↓
ISpecification<T> (Implementation)
    ↓
IQueryable<T> (Filtered, Sorted, Paginated)
```

---

### 2. **Infrastructure Layer** (Data Access)

**Location**: `src/MyApp.Shared/MyApp.Shared.Infrastructure/`

#### Key Components

- **`Repository<T, TKey>`** (`Repositories/Repository.cs`)
  - Extended with `QueryAsync(ISpecification<T> spec)` method
  - Applies specification to base query
  - Returns `PaginatedResult<T>` with metadata

- **`IRepository<T, TKey>`** (`Repositories/IRepository.cs`)
  - Updated interface with `QueryAsync` signature
  - Supports both simple pagination and specification-based queries

#### Implementation Pattern

```csharp
public async Task<PaginatedResult<TEntity>> QueryAsync(ISpecification<TEntity> spec)
{
    var baseQuery = _dbContext.Set<TEntity>().AsQueryable();
    
    var totalCount = await baseQuery.CountAsync();  // For pagination metadata
    var paginatedQuery = spec.Apply(baseQuery);      // Apply filters, sorting, pagination
    var items = await paginatedQuery.ToListAsync();
    
    return new PaginatedResult<TEntity>(items, pageNumber, pageSize, totalCount);
}
```

---

### 3. **Service/Domain Layer** (Business Rules)

**Location**: `src/MyApp.[Service]/MyApp.[Service].Domain/`

#### Entity-Specific Specifications

Each entity with list/search requirements should have a specification:

```csharp
// Example: ApplicationUserQuerySpec
public class ApplicationUserQuerySpec : BaseSpecification<ApplicationUser>
{
    public override IQueryable<ApplicationUser> Apply(IQueryable<ApplicationUser> query)
    {
        // 1. Apply entity-specific filters
        if (Query.Filters?.TryGetValue("isActive", out var isActiveStr) == true)
        {
            if (bool.TryParse(isActiveStr, out var isActive))
                query = query.Where(u => u.IsActive == isActive);
        }

        // 2. Apply search across fields
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(term))
            );
        }

        // 3. Apply pagination & sorting
        return ApplyPaginationAndSorting(query);
    }
}
```

---

### 4. **API Layer** (HTTP Endpoints)

**Location**: `src/MyApp.[Service]/MyApp.[Service].API/`

#### Controller Pattern

```csharp
[HttpGet("search")]
[HasPermission("Users", "Read")]
public async Task<ActionResult> SearchUsers([FromQuery] QuerySpec query)
{
    try
    {
        // 1. Validate query
        query.Validate();

        // 2. Create specification
        var spec = new ApplicationUserQuerySpec(query);

        // 3. Execute query via repository
        var result = await _userRepository.QueryAsync(spec);

        // 4. Return with pagination metadata
        return Ok(new
        {
            data = result.Items,
            pagination = new
            {
                page = query.Page,
                pageSize = query.PageSize,
                total = result.TotalCount,
                totalPages = result.TotalPages,
                hasNextPage = result.HasNextPage,
                hasPreviousPage = result.HasPreviousPage
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Search error");
        return StatusCode(500, new { message = "An error occurred" });
    }
}
```

#### Extension Methods (`Extensions/QuerySpecExtensions.cs`)

```csharp
// Chain-able filters and sorting
query
    .WithFilter("isActive", "true")           // Add filter
    .WithDefaultSorting("createdAt", true)    // Set default sort
    .WithMaxPageSize(50);                     // Limit page size
```

---

### 5. **Frontend Layer** (React/Vite)

**Location**: `WebApp/src/`

#### Custom Hooks

- **`useQueryBuilder()`** (`hooks/useQuerySpec.ts`)
  - Manages QuerySpec state
  - Provides handlers: `addFilter()`, `removeFilter()`, `clearFilters()`, `setSort()`, `setSearch()`, `setPage()`, `setPageSize()`
  - Builds query strings for URLs
  - Chainable for convenience

- **`usePaginatedQuery<T>()`** 
  - Wraps `@tanstack/react-query` with backend API
  - Handles fetch, caching, errors
  - Returns: `data`, `isLoading`, `isError`, `error`

- **`useEntitySearch<T>()`** (Combination Hook)
  - Combines `useQueryBuilder()` and `usePaginatedQuery()`
  - Returns: `query`, `data`, `isLoading`, handlers
  - Ready-to-use for most list/search screens

#### Example Component

```tsx
const { query, data, isLoading, handlers } = useEntitySearch<User>('/api/users/search', {
  page: 1,
  pageSize: 20,
  sortBy: 'createdAt',
  sortDesc: true,
});

// Handlers available:
handlers.addFilter('isActive', 'true');
handlers.setSort('email', false);
handlers.setSearch('john');
handlers.setPage(2);
handlers.clearFilters();
```

---

## 📝 Query Parameter Format

### HTTP Query String

```
GET /api/users/search?page=1&pageSize=20&sortBy=createdAt&sortDesc=true&filters[isActive]=true&searchTerm=john
```

### Query Parts

| Parameter | Type | Example | Notes |
|-----------|------|---------|-------|
| `page` | int | `1` | 1-indexed, default: 1 |
| `pageSize` | int | `20` | Max: 100, default: 20 |
| `sortBy` | string | `createdAt` | Property name, optional |
| `sortDesc` | bool | `true` | Descending sort, default: false |
| `filters[key]` | string | `filters[isActive]=true` | Multiple filters supported |
| `searchTerm` | string | `john` | Search text across multiple fields |
| `searchFields` | string | `firstName,lastName,email` | Comma-separated field names |

### Response Format

```json
{
  "data": [
    { "id": "...", "email": "user@example.com", ... }
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

---

## 🔄 Data Flow Example

**Scenario**: User searches for active users with email containing "example", sorted by creation date (newest first), page 2

### Frontend
```tsx
const { handlers, query } = useEntitySearch<User>('/api/users/search');

handlers.addFilter('isActive', 'true');
handlers.setSearch('example');
handlers.setSort('createdAt', true);
handlers.setPage(2);

// Query state:
// {
//   page: 2,
//   pageSize: 20,
//   sortBy: 'createdAt',
//   sortDesc: true,
//   filters: { isActive: 'true' },
//   searchTerm: 'example'
// }

// URL built: /api/users/search?page=2&pageSize=20&sortBy=createdAt&sortDesc=true&filters[isActive]=true&searchTerm=example
```

### Backend
```csharp
[HttpGet("search")]
public async Task<ActionResult> SearchUsers([FromQuery] QuerySpec query)
{
    // query deserialized from URL parameters
    query.Validate();

    var spec = new ApplicationUserQuerySpec(query);
    // ApplicationUserQuerySpec.Apply() does:
    // 1. filter: u.IsActive == true
    // 2. search: u.Email.Contains("example") || u.FirstName.Contains("example") || ...
    // 3. sort: OrderByDescending(u => u.CreatedAt)
    // 4. paginate: Skip(20).Take(20)

    var result = await _userRepository.QueryAsync(spec);
    
    // Returns: PaginatedResult<User> with 20 items, TotalCount, etc.
    
    return Ok(result);
}
```

---

## 🛠️ Implementation Checklist

### For a New Entity

- [ ] **Domain**: Create `[Entity]QuerySpec : BaseSpecification<[Entity]>`
- [ ] **API**: Add `[HttpGet("search")]` endpoint accepting `QuerySpec`
- [ ] **API**: Register endpoint in route configuration
- [ ] **OpenAPI**: Add Swagger documentation with example queries
- [ ] **Tests**: Unit test specification filtering logic
- [ ] **Tests**: Integration test endpoint with various query combinations

### For Frontend

- [ ] Import `useEntitySearch<T>` hook
- [ ] Create filter UI components
- [ ] Bind handlers to UI events
- [ ] Display results with pagination controls
- [ ] Add loading/error states

---

## 💡 Best Practices

### Backend

1. **Validate Early**: Always call `query.Validate()` in endpoints
2. **Handle Nulls**: Check filter existence before parsing: `query.Filters?.TryGetValue(...)`
3. **Case-Insensitive Search**: Use `.ToLower()` for search terms
4. **Limit Page Size**: Enforce max 100 items per page
5. **Consistent Sorting**: Provide sensible defaults
6. **Performance**: Add database indexes on commonly sorted/filtered fields

### Frontend

1. **Debounce Search**: Add debounce to search input to reduce requests
2. **Cache Results**: Use React Query's caching (default: 5 minutes)
3. **URL Sync**: Persist query state in URL for shareable links
4. **Error Handling**: Show user-friendly error messages
5. **Loading States**: Display skeleton screens or spinners
6. **Accessibility**: Label filters and sort buttons properly

---

## 🚀 Performance Considerations

### Database Indexes

Create indexes on commonly queried/sorted fields:

```sql
-- For Users
CREATE INDEX idx_user_email ON [User](Email);
CREATE INDEX idx_user_isactive_createdat ON [User](IsActive, CreatedAt DESC);
CREATE INDEX idx_user_firstname ON [User](FirstName);
```

### Caching

- Frontend caches results by default (5 minutes stale time)
- Consider DAPR cache for frequently accessed searches
- Invalidate cache on mutations (add/update/delete)

### Query Optimization

- Use `AsNoTracking()` for read-only queries
- Select only needed columns with DTOs
- Avoid N+1 queries (use `.Include()` for related entities)
- Implement lazy loading for large related collections

---

## 📚 File Reference

### Domain Layer
- `src/MyApp.Shared/MyApp.Shared.Domain/Pagination/QuerySpec.cs` - Query DTO
- `src/MyApp.Shared/MyApp.Shared.Domain/Pagination/PaginatedResult.cs` - Result wrapper
- `src/MyApp.Shared/MyApp.Shared.Domain/Specifications/ISpecification.cs` - Base specification
- `src/MyApp.Shared/MyApp.Shared.Domain/Extensions/DynamicLinqExtensions.cs` - LINQ helpers

### Infrastructure Layer
- `src/MyApp.Shared/MyApp.Shared.Infrastructure/Repositories/Repository.cs` - Base repository with `QueryAsync`

### Service Layer
- `src/MyApp.Auth/MyApp.Auth.Domain/Specifications/ApplicationUserQuerySpec.cs` - Example specification

### API Layer
- `src/MyApp.Auth/MyApp.Auth.API/Extensions/QuerySpecExtensions.cs` - Controller helpers

### Frontend Layer
- `WebApp/src/hooks/useQuerySpec.ts` - React hooks
- `WebApp/src/components/UserSearchExample.tsx` - Example component

---

## 🔗 Related Documentation

- `docs/deployment/DEPLOYMENT.md` - How to deploy with all these features
- `docs/development/add-dependencies.prompt.md` - Adding dependencies
- `README.md` - Project overview

---

**Last Updated**: November 5, 2025  
**Version**: 1.0  
**Status**: ✅ Production Ready
