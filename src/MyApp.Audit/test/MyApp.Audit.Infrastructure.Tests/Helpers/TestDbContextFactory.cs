using Microsoft.EntityFrameworkCore;
using MyApp.Audit.Infrastructure;

namespace MyApp.Audit.Infrastructure.Tests.Helpers;

/// <summary>
/// Creates isolated in-memory <see cref="AuditSqlDbContext"/> instances for repository tests.
/// Each call produces a fresh database so tests never share state.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Returns a new <see cref="AuditSqlDbContext"/> backed by a uniquely-named
    /// in-memory database. Using a unique name per call guarantees test isolation.
    /// </summary>
    /// <returns>A configured <see cref="AuditSqlDbContext"/> with an empty in-memory database.</returns>
    public static AuditSqlDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AuditSqlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AuditSqlDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
