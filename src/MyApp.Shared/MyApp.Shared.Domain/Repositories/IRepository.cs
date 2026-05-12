using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using System.Linq.Expressions;

namespace MyApp.Shared.Domain.Repositories;

public interface IRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<PaginatedResult<TEntity>> GetAllPaginatedAsync(int pageNumber, int pageSize, IEnumerable<Expression<Func<TEntity?, object>>>? includes = null);

    /// <summary>
    /// Query entities using a specification for filtering, sorting, and pagination.
    /// </summary>
    Task<PaginatedResult<TEntity>> QueryAsync(ISpecification<TEntity> spec);

    Task<TEntity> AddAsync(TEntity entity);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
}
