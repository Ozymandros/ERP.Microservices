using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.XXXs.Domain.Entities;

namespace MyApp.XXXs.Infrastructure.Data;

public class XXXsDbContext : DbContext
{
    public XXXsDbContext(DbContextOptions<XXXsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.XXXConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.XXXLineConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

public class XXXsDbContextFactory : IDesignTimeDbContextFactory<XXXsDbContext>
{
    public XXXsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<XXXsDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=XXXsDb;Trusted_Connection=True;");

        return new XXXsDbContext(optionsBuilder.Options);
    }
}
