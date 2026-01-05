using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;

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

    public virtual async Task<PaginatedResult<TEntity>> GetAllPaginatedAsync(int pageNumber, int pageSize)
    {
        var paginationParams = new PaginationParams(pageNumber, pageSize);
        var query = _dbContext.Set<TEntity>();
        
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
        var baseQuery = _dbContext.Set<TEntity>().AsQueryable();
        
        // Get total count before pagination (for pagination metadata)
        var totalCount = await baseQuery.CountAsync();
        
        // Apply specification (filters, sorting, pagination)
        var paginatedQuery = spec.Apply(baseQuery);
        var items = await paginatedQuery.ToListAsync();

        // Extract pagination info from spec (assumes spec applies pagination)
        // Create PaginatedResult with the paginated items
        return new PaginatedResult<TEntity>(
            items,
            pageNumber: 1, // This will be overridden if we track page info in QuerySpec
            pageSize: items.Count,
            totalCount: totalCount
        );
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
