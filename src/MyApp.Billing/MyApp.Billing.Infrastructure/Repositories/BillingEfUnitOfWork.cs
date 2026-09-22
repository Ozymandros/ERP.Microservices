using Microsoft.EntityFrameworkCore;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Infrastructure.Persistence;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Billing.Infrastructure.Repositories;

/// <summary>
/// Billing-specific unit of work that corrects Payment entity states before commit.
/// </summary>
public sealed class BillingEfUnitOfWork : EfUnitOfWork
{
    private readonly BillingDbContext _context;

    /// <summary>
    /// Initializes a new instance of the BillingEfUnitOfWork class.
    /// </summary>
    /// <param name="context">The context.</param>
    public BillingEfUnitOfWork(BillingDbContext context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<EntityEntryDto>> CommitAsync(
        CancellationToken cancellationToken = default)
    {
        var paymentEntries = _context.ChangeTracker.Entries<Payment>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in paymentEntries)
        {
            var paymentId = entry.Entity.Id;
            var exists = await _context.Payments
                .AsNoTracking()
                .AnyAsync(p => p.Id == paymentId, cancellationToken);

            if (!exists)
                entry.State = EntityState.Added;
        }

        return await EntityChangeSnapshot.CommitAsync(_context, cancellationToken);
    }
}
