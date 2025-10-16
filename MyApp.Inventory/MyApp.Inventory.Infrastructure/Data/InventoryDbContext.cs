using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyApp.Inventorys.Infrastructure.Data;

public class InventorysDbContext : DbContext
{
    public InventorysDbContext(DbContextOptions<InventorysDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

public class InventorysDbContextFactory : IDesignTimeDbContextFactory<InventorysDbContext>
{
    public InventorysDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventorysDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=InventorysDb;Trusted_Connection=True;");

        return new InventorysDbContext(optionsBuilder.Options);
    }
}
