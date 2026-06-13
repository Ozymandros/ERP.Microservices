using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using System.Linq.Expressions;

namespace MyApp.Shared.Domain.Repositories;

/// <summary>
/// Generic repository contract for standard CRUD and query operations.
/// Persistence is committed via <see cref="IUnitOfWork.CommitAsync"/> from application services.
/// </summary>
/// <typeparam name="TEntity">The domain entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public interface IRepository<TEntity, TKey>
    where TEntity : class
{
    /// <summary>Gets an entity by primary key.</summary>
    Task<TEntity?> GetByIdAsync(TKey id);

    /// <summary>Gets all entities (no tracking).</summary>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>Gets a paginated list of entities.</summary>
    Task<PaginatedResult<TEntity>> GetAllPaginatedAsync(
        int pageNumber,
        int pageSize,
        IEnumerable<Expression<Func<TEntity, object>>>? includes = null);

    /// <summary>Query entities using a specification for filtering, sorting, and pagination.</summary>
    Task<PaginatedResult<TEntity>> QueryAsync(ISpecification<TEntity> spec);

    /// <summary>Stages an entity for insert (does not commit).</summary>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>Stages an entity for update (does not commit).</summary>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>Stages an entity for delete (does not commit).</summary>
    Task DeleteAsync(TEntity entity);
}
