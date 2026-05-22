using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using System.Linq.Expressions;

namespace MyApp.Shared.Domain.Repositories;

/// <summary>
/// Marker for repositories that expose a unit-of-work save operation with change summaries.
/// </summary>
public interface IRepository
{
    /// <summary>
    /// Persists all pending changes and returns a summary of affected entities.
    /// </summary>
    /// <param name="disableTracking">Whether to disable change tracking for this operation.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>Change summaries captured immediately before persistence.</returns>
    Task<IReadOnlyCollection<EntityEntryDto>> SaveChangesAsync(bool disableTracking = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic repository contract for standard CRUD and query operations.
/// </summary>
/// <typeparam name="TEntity">The domain entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public interface IRepository<TEntity, TKey> : IRepository
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<PaginatedResult<TEntity>> GetAllPaginatedAsync(
        int pageNumber,
        int pageSize,
        IEnumerable<Expression<Func<TEntity?, object>>>? includes = null);

    /// <summary>
    /// Query entities using a specification for filtering, sorting, and pagination.
    /// </summary>
    Task<PaginatedResult<TEntity>> QueryAsync(ISpecification<TEntity> spec);

    Task<TEntity> AddAsync(TEntity entity);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
}
