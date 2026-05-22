using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyApp.Shared.Infrastructure.Repositories;

/// <summary>
/// Base class for EF Core repositories that centralizes access to the database context
/// and provides a virtual hook for persisting unit-of-work changes.
/// </summary>
/// <remarks>
/// Derived types should call <see cref="SaveChangesAsync"/> instead of invoking
/// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> directly so cross-cutting
/// behavior can be applied consistently.
/// </remarks>
public abstract class DbContextRepositoryBase : IRepository
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Gets the EF Core database context used by this repository.
    /// </summary>
    protected readonly DbContext DbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextRepositoryBase"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core context for the service database.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="dbContext"/> is <see langword="null"/>.
    /// </exception>
    protected DbContextRepositoryBase(DbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyCollection<EntityEntryDto>> SaveChangesAsync(bool disableTracking = false, CancellationToken cancellationToken = default)
    {
        if (disableTracking)
        {
            await DbContext.SaveChangesAsync(false, cancellationToken);
            return [];
        }

        var entries = DbContext.ChangeTracker
            .Entries()
            .Where(e =>
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        // Capture the state and property snapshots BEFORE SaveChanges so original values
        // and EF state are still meaningful. EntityId for Added rows may not be assigned
        // yet (store-generated keys), so we resolve it again AFTER SaveChanges below.
        var snapshots = entries
            .Select(entry =>
            {
                var (originalJson, newJson) = ResolveEntitySnapshots(entry);
                return new
                {
                    Entry = entry,
                    EntityName = entry.Metadata.ClrType.Name,
                    State = entry.State.ToString(),
                    Properties = GetPropertyChanges(entry),
                    OriginalValue = originalJson,
                    NewValue = newJson
                };
            })
            .ToList();

        await DbContext.SaveChangesAsync(false, cancellationToken);

        return snapshots
            .Select(s => new EntityEntryDto(
                s.EntityName,
                ResolvePrimaryKey(s.Entry),
                s.State,
                s.Properties,
                s.OriginalValue,
                s.NewValue))
            .ToList();
    }

    private static (string? OriginalValue, string? NewValue) ResolveEntitySnapshots(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Added => (null, BuildEntitySnapshotJson(entry, useOriginalValues: false)),
            EntityState.Deleted => (BuildEntitySnapshotJson(entry, useOriginalValues: true), null),
            EntityState.Modified => (
                BuildEntitySnapshotJson(entry, useOriginalValues: true),
                BuildEntitySnapshotJson(entry, useOriginalValues: false)),
            _ => (null, null)
        };
    }

    private static string? BuildEntitySnapshotJson(EntityEntry entry, bool useOriginalValues)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
                continue;

            dict[property.Metadata.Name] = useOriginalValues
                ? property.OriginalValue
                : property.CurrentValue;
        }

        return dict.Count == 0 ? null : JsonSerializer.Serialize(dict, SnapshotJsonOptions);
    }

    private static object? ResolvePrimaryKey(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is null)
            return null;

        var keyProperties = primaryKey.Properties;
        if (keyProperties.Count == 0)
            return null;

        if (keyProperties.Count == 1)
        {
            return entry.Property(keyProperties[0].Name).CurrentValue;
        }

        // Composite key: join individual values with '|' as a stable string representation.
        var parts = keyProperties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty);
        return string.Join("|", parts);
    }

    private static IReadOnlyCollection<PropertyChangeEntryDto> GetPropertyChanges(EntityEntry entry)
    {
        var properties = new List<PropertyChangeEntryDto>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    properties.Add(new PropertyChangeEntryDto(
                        property.Metadata.Name,
                        null,
                        property.CurrentValue));
                    break;

                case EntityState.Deleted:
                    properties.Add(new PropertyChangeEntryDto(
                        property.Metadata.Name,
                        property.OriginalValue,
                        null));
                    break;

                case EntityState.Modified:
                    if (!property.IsModified)
                        continue;

                    var oldValue = property.OriginalValue;
                    var newValue = property.CurrentValue;

                    if (Equals(oldValue, newValue))
                        continue;

                    properties.Add(new PropertyChangeEntryDto(
                        property.Metadata.Name,
                        oldValue,
                        newValue));
                    break;
            }
        }

        return properties;
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
        await SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        DbSet.Update(entity);
        await SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(TEntity entity)
    {
        DbSet.Remove(entity);
        await SaveChangesAsync();
    }
}
