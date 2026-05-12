using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;
using System.Linq.Expressions;

namespace MyApp.Shared.Infrastructure.Repositories;

public abstract class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly DbContext _dbContext;

    protected Repository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id)
    {
        return await _dbContext.Set<TEntity>().FindAsync(id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbContext.Set<TEntity>().ToListAsync();
    }

    public virtual async Task<PaginatedResult<TEntity>> GetAllPaginatedAsync(int pageNumber, int pageSize, IEnumerable<Expression<Func<TEntity, object>>>? includes = null)
    {
        var paginationParams = new PaginationParams(pageNumber, pageSize);
        IQueryable<TEntity> query = _dbContext.Set<TEntity>();

        if (includes is not null)
            foreach (var includeExpression in includes)
            {
                query = query.Include(includeExpression);
            }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync();

        return new PaginatedResult<TEntity>(items, paginationParams.PageNumber, paginationParams.PageSize, totalCount);
    }

    /// <summary>
    /// Query entities using a specification for filtering, sorting, and pagination.
    /// </summary>
    /// <param name="spec">The specification defining the query logic</param>
    /// <returns>A paginated result with filtered and sorted items</returns>
    public virtual async Task<PaginatedResult<TEntity>> QueryAsync(ISpecification<TEntity> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var baseQuery = _dbContext.Set<TEntity>().AsNoTracking().AsQueryable();

        // 1. Apply only filters to get the total count of matching items (before pagination)
        var filteredQuery = spec.ApplyFilters(baseQuery);
        var totalCount = await filteredQuery.CountAsync();

        // 2. Apply the full specification (filters + sorting + pagination)
        var finalQuery = spec.Apply(baseQuery);
        var items = await finalQuery.ToListAsync();

        // 3. Extract pagination info from the spec if possible
        int pageNumber = 1;
        int pageSize = items.Count;

        if (spec is BaseSpecification<TEntity> baseSpec)
        {
            pageNumber = baseSpec.Query.Page;
            pageSize = baseSpec.Query.PageSize;
        }

        return new PaginatedResult<TEntity>(items, pageNumber, pageSize, totalCount);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await _dbContext.Set<TEntity>().AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        _dbContext.Set<TEntity>().Update(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        _dbContext.Set<TEntity>().Remove(entity);
        await _dbContext.SaveChangesAsync();
    }
}
