using Microsoft.EntityFrameworkCore;
using MyApp.Audit.Infrastructure;

namespace MyApp.Audit.Infrastructure.Tests.Helpers;

public static class TestDbContextFactory
{
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
