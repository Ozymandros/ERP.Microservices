using Microsoft.EntityFrameworkCore;
using MyApp.Crm.Infrastructure.Data;

namespace MyApp.Crm.Infrastructure.Tests.Helpers;

internal static class CrmTestDbContextFactory
{
    public static CrmDbContext Create()
    {
        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new CrmDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}