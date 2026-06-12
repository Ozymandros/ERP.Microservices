using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Shared.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/> for a single scoped <see cref="DbContext"/>.
/// </summary>
public class EfUnitOfWork : IUnitOfWork
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfUnitOfWork"/> class.
    /// </summary>
    /// <param name="dbContext">The service database context for this request scope.</param>
    public EfUnitOfWork(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public virtual Task<IReadOnlyCollection<EntityEntryDto>> CommitAsync(
        CancellationToken cancellationToken = default)
        => EntityChangeSnapshot.CommitAsync(_dbContext, cancellationToken);
}
