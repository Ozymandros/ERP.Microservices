using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;
using System.Linq.Expressions;

namespace MyApp.Shared.Infrastructure.Repositories;

/// <summary>
/// Base class for EF Core repositories. Provides access to the shared <see cref="DbContext"/>
/// for staging changes; commit via <see cref="IUnitOfWork"/> from application services.
/// </summary>
public abstract class DbContextRepositoryBase
{
    /// <summary>
    /// Gets the EF Core database context used by this repository.
    /// </summary>
    protected readonly DbContext DbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextRepositoryBase"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core context for the service database.</param>
    protected DbContextRepositoryBase(DbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }
}

/// <summary>
/// Generic EF Core repository that implements standard CRUD and query operations for an entity type.
/// </summary>
/// <typeparam name="TEntity">The domain entity type stored in the database.</typeparam>
/// <typeparam name="TKey">The type of the entity primary key.</typeparam>
public abstract class Repository<TEntity, TKey> : DbContextRepositoryBase, IRepository<TEntity, TKey>
    where TEntity : class
{
    /// <summary>
    /// Gets the tracked <see cref="DbSet{TEntity}"/> for this repository's entity type.
    /// </summary>
    protected virtual DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

    /// <summary>
    /// Gets a no-tracking queryable surface for read-only queries against this entity type.
    /// </summary>
    protected virtual IQueryable<TEntity> Queryable => DbContext.Set<TEntity>().AsNoTracking();

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core context for the service database.</param>
    protected Repository(DbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(TKey id)
    {
        return await DbSet.FindAsync(id);
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await Queryable.ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task<PaginatedResult<TEntity>> GetAllPaginatedAsync(
        int pageNumber,
        int pageSize,
        IEnumerable<Expression<Func<TEntity, object>>>? includes = null)
    {
        var paginationParams = new PaginationParams(pageNumber, pageSize);
        IQueryable<TEntity> query = DbContext.Set<TEntity>().AsNoTracking();

        if (includes is not null)
        {
            foreach (var includeExpression in includes)
            {
                query = query.Include(includeExpression);
            }
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync();

        return new PaginatedResult<TEntity>(items, paginationParams.PageNumber, paginationParams.PageSize, totalCount);
    }

    /// <inheritdoc />
    public virtual async Task<PaginatedResult<TEntity>> QueryAsync(ISpecification<TEntity> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var baseQuery = DbContext.Set<TEntity>().AsNoTracking().AsQueryable();

        var filteredQuery = spec.ApplyFilters(baseQuery);
        var totalCount = await filteredQuery.CountAsync();

        var finalQuery = spec.Apply(baseQuery);
        var items = await finalQuery.ToListAsync();

        int pageNumber = 1;
        int pageSize = items.Count;

        if (spec is BaseSpecification<TEntity> baseSpec)
        {
            pageNumber = baseSpec.Query.Page;
            pageSize = baseSpec.Query.PageSize;
        }

        return new PaginatedResult<TEntity>(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await DbSet.AddAsync(entity);
        return entity;
    }

    /// <inheritdoc />
    public virtual Task<TEntity> UpdateAsync(TEntity entity)
    {
        DbSet.Update(entity);
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public virtual Task DeleteAsync(TEntity entity)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
