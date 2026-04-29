using Microsoft.EntityFrameworkCore;
using MyApp.Billing.Infrastructure.Persistence;

namespace MyApp.Billing.Infrastructure.Tests.Helpers;

/// <summary>
/// Creates isolated in-memory BillingDbContext instances for repository tests.
/// Each call produces a fresh database so tests never share state.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Returns a new <see cref="BillingDbContext"/> backed by a uniquely-named
    /// in-memory database. Using a unique name per call guarantees test isolation.
    /// </summary>
    public static BillingDbContext Create()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new BillingDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
