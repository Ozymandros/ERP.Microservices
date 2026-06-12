using MyApp.Shared.Domain.Repositories;

namespace MyApp.Shared.Infrastructure.Repositories;

/// <summary>
/// No-op unit of work for services that extend AppServiceBase but do not persist via EF in the current scope.
/// </summary>
public sealed class NoOpUnitOfWork : IUnitOfWork
{
    /// <inheritdoc />
    public Task<IReadOnlyCollection<EntityEntryDto>> CommitAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<EntityEntryDto>>(Array.Empty<EntityEntryDto>());
}
